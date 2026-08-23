namespace Utano.Module.Identity.Domain.Entities;

public class EmailVerificationToken
{
    private EmailVerificationToken() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsValid => UsedAt is null && DateTimeOffset.UtcNow < ExpiresAt;

    internal static EmailVerificationToken Create(Guid userId, string tokenHash, int expiryMinutes)
    {
        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    internal void MarkUsed() => UsedAt = DateTimeOffset.UtcNow;
}
