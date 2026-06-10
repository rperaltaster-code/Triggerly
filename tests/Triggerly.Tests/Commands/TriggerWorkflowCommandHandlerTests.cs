using FluentAssertions;
using Moq;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Contracts;
using Triggerly.Shared.Models;
using Xunit;

namespace Triggerly.Tests.Commands;

public class TriggerWorkflowCommandHandlerTests
{
    private readonly Mock<IWorkflowRepository> _workflowRepo = new();
    private readonly Mock<IWorkflowExecutionRepository> _executionRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITemporalService> _temporalService = new();
    private readonly Mock<IAuditService> _auditMock = new();
    private readonly TriggerWorkflowCommandHandler _handler;

    public TriggerWorkflowCommandHandlerTests()
    {
        _auditMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new TriggerWorkflowCommandHandler(
            _workflowRepo.Object, _executionRepo.Object,
            _unitOfWork.Object, _temporalService.Object, _auditMock.Object);
    }

    private WorkflowDefinition CreateActiveWorkflow(string tenantId = "tenant-1")
    {
        var wf = WorkflowDefinition.Create("Test Workflow", "Desc", tenantId, "user-1");
        wf.AddStep("Step 1", StepType.Action, 0, []);
        wf.Activate();
        return wf;
    }

    [Fact]
    public async Task Handle_ActiveWorkflow_StartsExecutionAndReturnsDto()
    {
        var workflow = CreateActiveWorkflow();
        var command = new TriggerWorkflowCommand(workflow.Id, "tenant-1", "user-1", "Test User", null);
        const string runId = "run-abc-123";

        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(workflow.Id, default)).ReturnsAsync(workflow);
        _executionRepo.Setup(r => r.AddAsync(It.IsAny<WorkflowExecution>(), default)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _temporalService
            .Setup(t => t.StartWorkflowAsync(
                workflow.Id, It.IsAny<Guid>(), "tenant-1", It.IsAny<string>(), null,
                It.IsAny<List<WorkflowStepInput>>(), default))
            .ReturnsAsync(runId);
        _executionRepo.Setup(r => r.StartAsync(It.IsAny<Guid>(), runId, default)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.WorkflowId.Should().Be(workflow.Id);
        result.TemporalRunId.Should().Be(runId);
        result.Status.Should().Be(ExecutionStatus.Running);
    }

    [Fact]
    public async Task Handle_WorkflowNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(id, default)).ReturnsAsync((WorkflowDefinition?)null);
        var command = new TriggerWorkflowCommand(id, "tenant-1", "user-1", "Test User", null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_InactiveWorkflow_ThrowsInvalidOperationException()
    {
        var workflow = WorkflowDefinition.Create("Draft", "Desc", "tenant-1", "user");
        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(workflow.Id, default)).ReturnsAsync(workflow);
        var command = new TriggerWorkflowCommand(workflow.Id, "tenant-1", "user-1", "Test User", null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public async Task Handle_WrongTenant_ThrowsUnauthorizedAccessException()
    {
        var workflow = CreateActiveWorkflow("tenant-1");
        _workflowRepo.Setup(r => r.GetByIdWithStepsAsync(workflow.Id, default)).ReturnsAsync(workflow);
        var command = new TriggerWorkflowCommand(workflow.Id, "tenant-2", "user-1", "Test User", null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
