using MediatR;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Sso;

public record GetSsoConfigQuery(string TenantId) : IRequest<SsoConfigDto?>;
public record GetSsoPublicInfoQuery(string TenantId) : IRequest<SsoPublicDto?>;
