using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Team;

public record GetTeamQuery(string TenantId) : IRequest<List<TeamMemberDto>>;

public class GetTeamQueryHandler : IRequestHandler<GetTeamQuery, List<TeamMemberDto>>
{
    private readonly IUserRepository _users;
    private readonly ITenantRoleRepository _roles;

    public GetTeamQueryHandler(IUserRepository users, ITenantRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<List<TeamMemberDto>> Handle(GetTeamQuery request, CancellationToken cancellationToken)
    {
        var users = await _users.GetByTenantAsync(request.TenantId, cancellationToken);
        var roles = await _roles.GetByTenantAsync(request.TenantId, cancellationToken);
        var roleMap = roles.ToDictionary(r => r.UserId, r => r.Role.ToString());

        return users
            .Select(u => new TeamMemberDto(u.Id, u.Name, u.Email, roleMap.GetValueOrDefault(u.Id, "Viewer")))
            .OrderBy(m => m.Name)
            .ToList();
    }
}
