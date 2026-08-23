using System.Security.Cryptography;
using System.Text;

namespace Utano.Module.Identity.Features.Auth;

// Same rationale as PasswordResetTokenHasher - store only the hash so a read-only DB leak can't
// be redeemed directly. SHA256 is fine here since the raw token is high-entropy random, not
// user-guessable.
public static class EmailVerificationTokenHasher
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
