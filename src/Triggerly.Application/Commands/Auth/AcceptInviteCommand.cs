using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Auth;

public record AcceptInviteCommand(string Token, string Name, string Password) : IRequest<RegisterResponseDto>;

public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, RegisterResponseDto>
{
    private readonly ITeamInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly ITenantRoleRepository _roles;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;

    public AcceptInviteCommandHandler(
        ITeamInviteRepository invites,
        IUserRepository users,
        ITenantRoleRepository roles,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork uow)
    {
        _invites = invites;
        _users = users;
        _roles = roles;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _uow = uow;
    }

    public async Task<RegisterResponseDto> Handle(AcceptInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await _invites.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new InvalidOperationException("Invite not found or has already been used.");

        if (invite.IsAccepted)
            throw new InvalidOperationException("This invite has already been accepted.");

        if (invite.IsExpired)
            throw new InvalidOperationException("This invite has expired. Please ask your manager to send a new one.");

        if (await _users.ExistsByEmailAsync(invite.Email, cancellationToken))
            throw new InvalidOperationException("An account with this email already exists.");

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.CreateForTenant(request.Name, invite.Email, hash, invite.TenantId);

        await _users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var tenantRole = TenantRole.Create(user.Id, invite.TenantId, invite.Role);
        await _roles.AddAsync(tenantRole, cancellationToken);

        invite.Accept();
        await _uow.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(user, invite.Role.ToString());
        return new RegisterResponseDto(token, new AuthUserDto(user.Id, user.Name, user.Email, user.TenantId, invite.Role.ToString()));
    }
}
