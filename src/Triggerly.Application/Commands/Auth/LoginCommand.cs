using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(user);
        return new LoginResponseDto(token, new AuthUserDto(user.Id, user.Name, user.Email, user.TenantId));
    }
}
