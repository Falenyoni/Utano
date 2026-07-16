using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Identity.Domain.Entities;

public class Role : AggregateRoot
{
    private Role() { }

    private readonly List<RolePermission> _permissions = new();

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Role Create(Guid practiceId, string name, string? description, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Role name is required.");

        return new Role
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSystem = isSystem,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Role name is required.");
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetPermissions(IEnumerable<string> permissionKeys)
    {
        _permissions.Clear();
        foreach (var key in permissionKeys.Distinct())
            _permissions.Add(new RolePermission(Id, key));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<string> GetPermissionKeys() =>
        _permissions.Select(p => p.PermissionKey).ToList();

    public void Activate()
    {
        if (IsActive) throw new UtanoDomainException("Role is already active.");
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) throw new UtanoDomainException("Role is already inactive.");
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
