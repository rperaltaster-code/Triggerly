using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Team;

public record GetPendingInvitesQuery(string TenantId) : IRequest<List<TeamInviteDto>>;

public class GetPendingInvitesQueryHandler : IRequestHandler<GetPendingInvitesQuery, List<TeamInviteDto>>
{
    private readonly ITeamInviteRepository _invites;

    public GetPendingInvitesQueryHandler(ITeamInviteRepository invites) => _invites = invites;

    public async Task<List<TeamInviteDto>> Handle(GetPendingInvitesQuery request, CancellationToken cancellationToken)
    {
        var invites = await _invites.GetPendingByTenantAsync(request.TenantId, cancellationToken);
        return invites
            .Select(i => new TeamInviteDto(i.Id, i.Email, i.Role.ToString(), i.ExpiresAt, i.CreatedAt))
            .ToList();
    }
}
