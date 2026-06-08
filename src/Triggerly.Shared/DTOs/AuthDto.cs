namespace Triggerly.Shared.DTOs;

public record AuthUserDto(Guid Id, string Name, string Email, string TenantId);

public record LoginResponseDto(string Token, AuthUserDto User);

public record RegisterResponseDto(string Token, AuthUserDto User);
