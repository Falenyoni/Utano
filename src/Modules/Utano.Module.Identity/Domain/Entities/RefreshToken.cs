namespace Utano.Module.Identity.Domain.Entities;

public class RefreshToken
{
    private RefreshToken() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    internal static RefreshToken Create(Guid userId, string token, int expiryDays)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expiryDays),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    internal void Revoke() => IsRevoked = true;
}
