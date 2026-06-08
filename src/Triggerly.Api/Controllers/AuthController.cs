using MediatR;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.Auth;

namespace Triggerly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new RegisterCommand(request.Name, request.Email, request.Password), cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new LoginCommand(request.Email, request.Password), cancellationToken);
        return Ok(result);
    }
}

public record RegisterRequest(string Name, string Email, string Password);
public record LoginRequest(string Email, string Password);
