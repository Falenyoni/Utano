namespace Utano.Module.Identity.Domain.Entities;

// Canonical catalog of every permission key the system recognizes. RolePermission.PermissionKey
// has a real FK into this table now, so a typo'd or orphaned key is rejected at write time
// instead of silently inserting and never matching anything.
public class Permission
{
    private Permission() { }

    public Permission(string key, string? description = null)
    {
        Key = key;
        Description = description;
    }

    public string Key { get; private set; } = null!;
    public string? Description { get; private set; }
}
