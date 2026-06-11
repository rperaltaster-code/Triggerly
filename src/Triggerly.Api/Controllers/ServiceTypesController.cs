using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/service-types")]
public class ServiceTypesController : ControllerBase
{
    private readonly IServiceTypeRepository _repo;
    private readonly IUnitOfWork _uow;

    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException();

    public ServiceTypesController(IServiceTypeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default)
    {
        var items = await _repo.GetByTenantAsync(TenantId, ct);
        return Ok(items.Select(MapDto));
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveServiceTypeRequest request, CancellationToken ct = default)
    {
        var st = ServiceType.Create(TenantId, request.Name, request.Description,
            request.DefaultWorkflowId, request.DefaultFilingPeriod, request.Color);
        await _repo.AddAsync(st, ct);
        await _uow.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), MapDto(st));
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveServiceTypeRequest request, CancellationToken ct = default)
    {
        var st = await _repo.GetByIdAsync(TenantId, id, ct);
        if (st == null) return NotFound();
        st.Update(request.Name, request.Description, request.DefaultWorkflowId, request.DefaultFilingPeriod, request.Color);
        await _uow.SaveChangesAsync(ct);
        return Ok(MapDto(st));
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var st = await _repo.GetByIdAsync(TenantId, id, ct);
        if (st == null) return NotFound();
        _repo.Remove(st);
        await _uow.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ServiceTypeDto MapDto(ServiceType st) =>
        new(st.Id, st.TenantId, st.Name, st.Description, st.DefaultWorkflowId, st.DefaultFilingPeriod, st.Color, st.CreatedAt);
}
