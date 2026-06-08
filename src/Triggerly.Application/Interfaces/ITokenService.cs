using Triggerly.Domain.Entities;

namespace Triggerly.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
