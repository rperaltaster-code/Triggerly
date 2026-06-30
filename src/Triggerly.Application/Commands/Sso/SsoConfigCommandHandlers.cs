using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Sso;

public class SaveSsoConfigCommandHandler : IRequestHandler<SaveSsoConfigCommand, SsoConfigDto>
{
    private readonly ITenantSsoConfigRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;

    public SaveSsoConfigCommandHandler(ITenantSsoConfigRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task<SsoConfigDto> Handle(SaveSsoConfigCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByTenantAsync(request.TenantId, cancellationToken);

        TenantSsoConfig config;
        if (existing is null)
        {
            config = TenantSsoConfig.Create(
                request.TenantId, request.ClientId, request.ClientSecret,
                request.DirectoryTenantId, request.GroupClaimName, request.GroupRoleMappings);
            await _repository.AddAsync(config, cancellationToken);
        }
        else
        {
            existing.Update(request.ClientId, request.ClientSecret,
                request.DirectoryTenantId, request.GroupClaimName, request.GroupRoleMappings);
            config = existing;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            "SsoConfigSaved", "SsoConfig", config.Id.ToString(), "SSO Configuration",
            ct: cancellationToken);

        return SsoConfigMapper.ToDto(config);
    }
}

public class DeleteSsoConfigCommandHandler : IRequestHandler<DeleteSsoConfigCommand>
{
    private readonly ITenantSsoConfigRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;

    public DeleteSsoConfigCommandHandler(ITenantSsoConfigRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task Handle(DeleteSsoConfigCommand request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.TenantId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            "SsoConfigDeleted", "SsoConfig", string.Empty, "SSO Configuration",
            ct: cancellationToken);
    }
}

public class ToggleSsoConfigCommandHandler : IRequestHandler<ToggleSsoConfigCommand>
{
    private readonly ITenantSsoConfigRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;

    public ToggleSsoConfigCommandHandler(ITenantSsoConfigRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task Handle(ToggleSsoConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTenantAsync(request.TenantId, cancellationToken)
            ?? throw new KeyNotFoundException("SSO configuration not found.");

        config.SetEnabled(request.Enable);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            request.Enable ? "SsoEnabled" : "SsoDisabled", "SsoConfig", config.Id.ToString(), "SSO Configuration",
            ct: cancellationToken);
    }
}

internal static class SsoConfigMapper
{
    internal static SsoConfigDto ToDto(TenantSsoConfig c) =>
        new(c.Id, c.Provider, c.ClientId, c.DirectoryTenantId, c.GroupClaimName, c.GroupRoleMappings, c.IsEnabled);
}

// file-scope helpers so the handler class can call ToDto directly
file static class Ext
{
    internal static SsoConfigDto ToDto(this TenantSsoConfig c) => SsoConfigMapper.ToDto(c);
}
