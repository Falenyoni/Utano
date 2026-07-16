using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Domain.Interfaces;

public interface ITokenService
{
    string GenerateJwtToken(User user, IEnumerable<string> permissions);
    string GenerateRefreshToken();
}
