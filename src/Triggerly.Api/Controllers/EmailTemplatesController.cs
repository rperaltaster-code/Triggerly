using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Services;
using Triggerly.Shared.DTOs;

namespace Triggerly.Api.Controllers;

[Authorize(Roles = "Manager")]
[ApiController]
[Route("api/email-templates")]
public class EmailTemplatesController : ControllerBase
{
    private readonly IEmailTemplateRepository _repository;
    private readonly IUnitOfWork _uow;
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException();

    public EmailTemplatesController(IEmailTemplateRepository repository, IUnitOfWork uow)
    {
        _repository = repository;
        _uow = uow;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var customs = await _repository.GetAllForTenantAsync(TenantId, cancellationToken);
        var customMap = customs.ToDictionary(t => t.TemplateKey);
        var defaults = EmailTemplateService.GetDefaults();

        var result = defaults.Select(kvp =>
        {
            customMap.TryGetValue(kvp.Key, out var custom);
            return new EmailTemplateDto(
                kvp.Key,
                custom?.Subject ?? kvp.Value.Subject,
                custom?.Body ?? kvp.Value.Body,
                custom != null,
                custom?.UpdatedAt);
        }).ToList();

        return Ok(result);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Upsert(
        string key,
        [FromBody] UpsertEmailTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var defaults = EmailTemplateService.GetDefaults();
        if (!defaults.ContainsKey(key))
            return BadRequest($"Unknown template key '{key}'.");

        var existing = await _repository.GetAsync(TenantId, key, cancellationToken);
        if (existing != null)
        {
            existing.Update(request.Subject, request.Body);
        }
        else
        {
            var template = EmailTemplate.Create(TenantId, key, request.Subject, request.Body);
            await _repository.AddAsync(template, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Reset(string key, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetAsync(TenantId, key, cancellationToken);
        if (existing == null)
            return NoContent(); // already default

        _repository.Remove(existing);
        await _uow.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record UpsertEmailTemplateRequest(string Subject, string Body);
