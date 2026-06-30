using MediatR;
using Triggerly.Application.Commands.Sso;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Sso;

public class GetSsoConfigQueryHandler : IRequestHandler<GetSsoConfigQuery, SsoConfigDto?>
{
    private readonly ITenantSsoConfigRepository _repository;
    public GetSsoConfigQueryHandler(ITenantSsoConfigRepository repository) => _repository = repository;

    public async Task<SsoConfigDto?> Handle(GetSsoConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return config is null ? null : SsoConfigMapper.ToDto(config);
    }
}

public class GetSsoPublicInfoQueryHandler : IRequestHandler<GetSsoPublicInfoQuery, SsoPublicDto?>
{
    private readonly ITenantSsoConfigRepository _repository;
    public GetSsoPublicInfoQueryHandler(ITenantSsoConfigRepository repository) => _repository = repository;

    public async Task<SsoPublicDto?> Handle(GetSsoPublicInfoQuery request, CancellationToken cancellationToken)
    {
        var config = await _repository.GetByTenantAsync(request.TenantId, cancellationToken);
        return config is null ? null : new SsoPublicDto(config.Provider, config.IsEnabled);
    }
}
