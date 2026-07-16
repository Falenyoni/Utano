namespace Utano.Module.Identity.Domain.Entities;

public class UserRoleAssignment
{
    private UserRoleAssignment() { }

    public UserRoleAssignment(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    public Role Role { get; private set; } = null!;
}
