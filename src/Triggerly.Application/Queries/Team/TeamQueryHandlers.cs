using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Team;

public record GetTeamQuery(string TenantId) : IRequest<List<TeamMemberDto>>;

public record GetTeamWorkloadQuery(string TenantId) : IRequest<List<TeamWorkloadDto>>;

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

public class GetTeamWorkloadQueryHandler : IRequestHandler<GetTeamWorkloadQuery, List<TeamWorkloadDto>>
{
    private readonly IUserRepository _users;
    private readonly ITenantRoleRepository _roles;
    private readonly IWorkflowExecutionRepository _executions;

    public GetTeamWorkloadQueryHandler(
        IUserRepository users,
        ITenantRoleRepository roles,
        IWorkflowExecutionRepository executions)
    {
        _users = users;
        _roles = roles;
        _executions = executions;
    }

    public async Task<List<TeamWorkloadDto>> Handle(GetTeamWorkloadQuery request, CancellationToken cancellationToken)
    {
        var users = await _users.GetByTenantAsync(request.TenantId, cancellationToken);
        var roles = await _roles.GetByTenantAsync(request.TenantId, cancellationToken);
        var roleMap = roles.ToDictionary(r => r.UserId, r => r.Role.ToString());
        var taskCounts = await _executions.GetOpenTaskCountsByUserAsync(request.TenantId, cancellationToken);

        return users
            .Select(u => new TeamWorkloadDto(
                u.Id, u.Name, u.Email,
                roleMap.GetValueOrDefault(u.Id, "Preparer"),
                taskCounts.GetValueOrDefault(u.Id, 0)))
            .OrderByDescending(m => m.OpenTaskCount)
            .ThenBy(m => m.Name)
            .ToList();
    }
}
