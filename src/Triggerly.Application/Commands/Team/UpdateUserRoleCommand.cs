using MediatR;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Team;

public record UpdateUserRoleCommand(Guid TargetUserId, string TenantId, UserRole NewRole) : IRequest;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand>
{
    private readonly ITenantRoleRepository _roles;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserRoleCommandHandler(ITenantRoleRepository roles, IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var existing = await _roles.GetAsync(request.TargetUserId, request.TenantId, cancellationToken);
        if (existing is null)
        {
            var newRole = TenantRole.Create(request.TargetUserId, request.TenantId, request.NewRole);
            await _roles.AddAsync(newRole, cancellationToken);
        }
        else
        {
            existing.UpdateRole(request.NewRole);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
