using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _clients;
    private readonly IClientServiceRepository _clientServices;
    private readonly IServiceTypeRepository _serviceTypes;
    private readonly IWorkflowRepository _workflows;
    private readonly IUnitOfWork _uow;

    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException();

    public ClientsController(
        IClientRepository clients,
        IClientServiceRepository clientServices,
        IServiceTypeRepository serviceTypes,
        IWorkflowRepository workflows,
        IUnitOfWork uow)
    {
        _clients = clients;
        _clientServices = clientServices;
        _serviceTypes = serviceTypes;
        _workflows = workflows;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _clients.GetPagedAsync(TenantId, page, pageSize, search, ct);
        var dtos = items.Select(c => new ClientSummaryDto(c.Id, c.Name, c.Email, c.Phone, c.Services.Count, c.UpdatedAt)).ToList();
        return Ok(new { items = dtos, totalCount = total, page, pageSize });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(TenantId, id, ct);
        if (client == null) return NotFound();
        return Ok(MapClient(client));
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveClientRequest request, CancellationToken ct = default)
    {
        var client = Client.Create(TenantId, request.Name, request.Email, request.Phone, request.BalanceDate, request.IrdNumber, request.Notes);
        await _clients.AddAsync(client, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = client.Id }, MapClient(client));
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveClientRequest request, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(TenantId, id, ct);
        if (client == null) return NotFound();
        client.Update(request.Name, request.Email, request.Phone, request.BalanceDate, request.IrdNumber, request.Notes);
        await _uow.SaveChangesAsync(ct);
        return Ok(MapClient(client));
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(TenantId, id, ct);
        if (client == null) return NotFound();
        _clients.Remove(client);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    // --- Client Services ---

    [HttpGet("{clientId:guid}/services")]
    public async Task<IActionResult> GetServices(Guid clientId, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(TenantId, clientId, ct);
        if (client == null) return NotFound();

        var services = await _clientServices.GetByClientAsync(clientId, ct);
        var serviceTypes = await _serviceTypes.GetByTenantAsync(TenantId, ct);
        var stMap = serviceTypes.ToDictionary(st => st.Id);

        var dtos = new List<ClientServiceDto>();
        foreach (var svc in services)
        {
            stMap.TryGetValue(svc.ServiceTypeId, out var st);
            var workflow = await _workflows.GetByIdAsync(svc.WorkflowId, ct);
            dtos.Add(new ClientServiceDto(svc.Id, svc.ClientId, svc.ServiceTypeId,
                st?.Name ?? "Unknown", svc.WorkflowId, workflow?.Name,
                svc.FilingPeriod, svc.LastFiledAt, svc.NextDueAt, svc.IsActive, svc.Notes));
        }
        return Ok(dtos);
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPost("{clientId:guid}/services")]
    public async Task<IActionResult> AddService(Guid clientId, [FromBody] SaveClientServiceRequest request, CancellationToken ct = default)
    {
        var client = await _clients.GetByIdAsync(TenantId, clientId, ct);
        if (client == null) return NotFound();

        var svc = ClientService.Create(clientId, request.ServiceTypeId, request.WorkflowId, request.FilingPeriod, request.Notes);
        await _clientServices.AddAsync(svc, ct);
        await _uow.SaveChangesAsync(ct);
        return Created(string.Empty, svc.Id);
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPut("{clientId:guid}/services/{serviceId:guid}")]
    public async Task<IActionResult> UpdateService(Guid clientId, Guid serviceId, [FromBody] SaveClientServiceRequest request, CancellationToken ct = default)
    {
        var svc = await _clientServices.GetByIdAsync(serviceId, ct);
        if (svc == null || svc.ClientId != clientId) return NotFound();
        svc.Update(request.WorkflowId, request.FilingPeriod, request.IsActive, request.Notes);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{clientId:guid}/services/{serviceId:guid}")]
    public async Task<IActionResult> RemoveService(Guid clientId, Guid serviceId, CancellationToken ct = default)
    {
        var svc = await _clientServices.GetByIdAsync(serviceId, ct);
        if (svc == null || svc.ClientId != clientId) return NotFound();
        _clientServices.Remove(svc);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ClientDto MapClient(Client c) =>
        new(c.Id, c.TenantId, c.Name, c.Email, c.Phone, c.BalanceDate, c.IrdNumber, c.Notes, c.ExternalId, c.Source, c.CreatedAt, c.UpdatedAt);
}
