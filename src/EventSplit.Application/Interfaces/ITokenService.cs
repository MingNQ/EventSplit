using EventSplit.Domain.Entities;

namespace EventSplit.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
}
