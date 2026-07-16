namespace Utano.Module.Identity.Domain.Entities;

public class RolePermission
{
    private RolePermission() { }

    public RolePermission(Guid roleId, string permissionKey)
    {
        RoleId = roleId;
        PermissionKey = permissionKey;
    }

    public Guid RoleId { get; private set; }
    public string PermissionKey { get; private set; } = null!;
}
