using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Team;

public record InviteTeamMemberCommand(
    string TenantId,
    string Email,
    UserRole Role,
    string InviterName,
    string BaseUrl) : IRequest;

public class InviteTeamMemberCommandHandler : IRequestHandler<InviteTeamMemberCommand>
{
    private readonly ITeamInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly IEmailService _email;
    private readonly IUnitOfWork _uow;

    public InviteTeamMemberCommandHandler(
        ITeamInviteRepository invites,
        IUserRepository users,
        IEmailService email,
        IUnitOfWork uow)
    {
        _invites = invites;
        _users = users;
        _email = email;
        _uow = uow;
    }

    public async Task Handle(InviteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        if (await _users.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new InvalidOperationException("A user with this email already exists.");

        if (await _invites.HasPendingInviteAsync(request.TenantId, request.Email, cancellationToken))
            throw new InvalidOperationException("A pending invite already exists for this email.");

        var invite = TeamInvite.Create(request.TenantId, request.Email, request.Role);
        await _invites.AddAsync(invite, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var link = $"{request.BaseUrl}/accept-invite?token={invite.Token}";
        await _email.SendAsync(
            request.Email,
            "You've been invited to join Triggerly",
            $"""
            <p><strong>{request.InviterName}</strong> has invited you to join their team on Triggerly as a <strong>{request.Role}</strong>.</p>
            <p><a href="{link}">Accept Invitation</a></p>
            <p>This invite link expires in 7 days. If you weren't expecting this, you can ignore this email.</p>
            """,
            cancellationToken);
    }
}
