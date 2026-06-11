using MediatR;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Application.Commands.Team;

public record RevokeInviteCommand(Guid InviteId, string TenantId) : IRequest;

public class RevokeInviteCommandHandler : IRequestHandler<RevokeInviteCommand>
{
    private readonly ITeamInviteRepository _invites;
    private readonly IUnitOfWork _uow;

    public RevokeInviteCommandHandler(ITeamInviteRepository invites, IUnitOfWork uow)
    {
        _invites = invites;
        _uow = uow;
    }

    public async Task Handle(RevokeInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _invites.GetByIdAsync(request.InviteId, cancellationToken)
            ?? throw new InvalidOperationException("Invite not found.");

        if (invite.TenantId != request.TenantId)
            throw new UnauthorizedAccessException();

        _invites.Remove(invite);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
