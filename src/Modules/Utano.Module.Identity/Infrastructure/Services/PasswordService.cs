using Microsoft.AspNetCore.Identity;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(string.Empty, password);

    public bool Verify(string password, string hash) =>
        _hasher.VerifyHashedPassword(string.Empty, hash, password)
            != PasswordVerificationResult.Failed;
}
