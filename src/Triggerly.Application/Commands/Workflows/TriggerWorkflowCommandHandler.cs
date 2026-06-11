using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Contracts;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;
using Triggerly.Shared.Utils;

namespace Triggerly.Application.Commands.Workflows;

public class TriggerWorkflowCommandHandler : IRequestHandler<TriggerWorkflowCommand, WorkflowExecutionDto>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IWorkflowVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemporalService _temporalService;
    private readonly IAuditService _audit;
    private readonly IClientRepository _clients;
    private readonly IClientServiceRepository _clientServices;
    private readonly IServiceTypeRepository _serviceTypes;

    public TriggerWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IWorkflowVersionRepository versionRepository,
        IUnitOfWork unitOfWork,
        ITemporalService temporalService,
        IAuditService audit,
        IClientRepository clients,
        IClientServiceRepository clientServices,
        IServiceTypeRepository serviceTypes)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
        _temporalService = temporalService;
        _audit = audit;
        _clients = clients;
        _clientServices = clientServices;
        _serviceTypes = serviceTypes;
    }

    public async Task<WorkflowExecutionDto> Handle(TriggerWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdWithStepsAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        if (workflow.Status != WorkflowStatus.Active)
            throw new InvalidOperationException("Only active workflows can be triggered.");

        var latestVersion = await _versionRepository.GetLatestByWorkflowAsync(workflow.Id, cancellationToken);

        // Build enriched InputData with client/service tokens if triggered for a client
        var inputData = new Dictionary<string, object>(request.InputData ?? []);
        string? clientName = null;
        string? serviceTypeName = null;

        if (request.ClientId.HasValue && request.ClientServiceId.HasValue)
        {
            var client = await _clients.GetByIdAsync(request.TenantId, request.ClientId.Value, cancellationToken);
            var svc = await _clientServices.GetByIdAsync(request.ClientServiceId.Value, cancellationToken);

            if (client != null)
            {
                clientName = client.Name;
                inputData["client.name"] = client.Name;
                inputData["client.email"] = client.Email;
                inputData["client.phone"] = client.Phone ?? string.Empty;
                inputData["client.irdNumber"] = client.IrdNumber ?? string.Empty;
                inputData["client.balanceDate"] = client.BalanceDate ?? string.Empty;
            }

            if (svc != null)
            {
                var serviceType = await _serviceTypes.GetByIdAsync(request.TenantId, svc.ServiceTypeId, cancellationToken);
                serviceTypeName = serviceType?.Name;
                inputData["service.name"] = serviceType?.Name ?? string.Empty;
                inputData["service.filingPeriod"] = svc.FilingPeriod.ToString();
                inputData["service.nextDueAt"] = svc.NextDueAt?.ToString("yyyy-MM-dd") ?? string.Empty;
            }
        }

        var temporalWorkflowId = $"triggerly-{workflow.Id}-{Guid.NewGuid():N}";

        var execution = WorkflowExecution.Create(
            workflow.Id,
            temporalWorkflowId,
            request.TenantId,
            request.TriggeredByName ?? request.TriggeredBy,
            inputData);

        if (latestVersion is not null)
            execution.SetVersion(latestVersion.Id, latestVersion.VersionNumber);

        if (request.ClientId.HasValue && request.ClientServiceId.HasValue)
            execution.SetClient(request.ClientId.Value, request.ClientServiceId.Value);

        await _executionRepository.AddAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var steps = workflow.Steps
            .Select(s => new WorkflowStepInput(s.Id, s.Name, s.Type.ToString(), s.Order, s.Config, s.ApproverEmail, s.NextStepId))
            .ToList();

        var runId = await _temporalService.StartWorkflowAsync(
            workflow.Id,
            execution.Id,
            request.TenantId,
            workflow.Name,
            inputData,
            steps,
            cancellationToken);

        await _executionRepository.StartAsync(execution.Id, runId, cancellationToken);

        await _audit.LogAsync(request.TenantId,
            request.TriggeredBy ?? "system",
            request.TriggeredByName ?? request.TriggeredBy ?? "System",
            "ExecutionTriggered", "Execution", execution.Id.ToString(), workflow.Name,
            ct: cancellationToken);

        return MapToDto(execution, workflow.Name, runId, clientName, serviceTypeName);
    }

    private static WorkflowExecutionDto MapToDto(WorkflowExecution execution, string workflowName, string runId,
        string? clientName = null, string? serviceTypeName = null) =>
        new(
            execution.Id,
            execution.WorkflowId,
            workflowName,
            execution.TemporalWorkflowId,
            runId,
            ExecutionStatus.Running,
            execution.TenantId,
            execution.TriggeredBy,
            execution.InputData,
            execution.OutputData,
            execution.ErrorMessage,
            execution.CurrentStepOrder,
            execution.CurrentStepName,
            execution.StartedAt,
            execution.CompletedAt,
            execution.SlaBreachedAt,
            [],
            [],
            execution.WorkflowVersionNumber,
            execution.ClientId,
            execution.ClientServiceId,
            clientName,
            serviceTypeName);
}
