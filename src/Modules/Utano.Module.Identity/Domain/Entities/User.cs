using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.ValueObjects;

namespace Utano.Module.Identity.Domain.Entities;

public class User : AggregateRoot
{
    private User() { }

    private readonly List<RefreshToken> _refreshTokens = new();

    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }

    public string FullName => $"{FirstName} {LastName}";
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static User Create(Guid practiceId, string firstName, string lastName,
        string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new UtanoDomainException("First name is required.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new UtanoDomainException("Last name is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new UtanoDomainException("Password is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = Email.Create(email),
            PasswordHash = passwordHash,
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public RefreshToken AddRefreshToken(string token, int expiryDays)
    {
        // revoke all existing active tokens before issuing new one
        foreach (var t in _refreshTokens.Where(t => t.IsActive))
            t.Revoke();

        var refreshToken = RefreshToken.Create(Id, token, expiryDays);
        _refreshTokens.Add(refreshToken);
        UpdatedAt = DateTimeOffset.UtcNow;
        return refreshToken;
    }

    public bool TryRevokeRefreshToken(string token)
    {
        var existing = _refreshTokens.FirstOrDefault(t => t.Token == token && t.IsActive);
        if (existing is null) return false;
        existing.Revoke();
        UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public void Update(string firstName, string lastName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new UtanoDomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new UtanoDomainException("Last name is required.");
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Role = role;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status == UserStatus.Active)
            throw new UtanoDomainException("User is already active.");
        Status = UserStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
            throw new UtanoDomainException("User is already inactive.");
        Status = UserStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
