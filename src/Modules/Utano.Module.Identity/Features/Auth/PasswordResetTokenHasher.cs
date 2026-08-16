using System.Security.Cryptography;
using System.Text;

namespace Utano.Module.Identity.Features.Auth;

// Reset tokens are stored hashed (not raw) so a read-only DB leak can't be used to redeem them
// directly - SHA256 is sufficient here (not a slow hash like the password hash) since the raw
// token is a high-entropy random value, not user-guessable low-entropy input.
public static class PasswordResetTokenHasher
{
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
