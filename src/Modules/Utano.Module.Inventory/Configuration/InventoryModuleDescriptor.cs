using Utano.Module.Core.Modules;

namespace Utano.Module.Inventory.Configuration;

internal sealed class InventoryModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "inventory";
    public string Plan => "professional";

    public IReadOnlyList<string> AllPermissions => [View, Manage];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin => [View, Manage],
        SystemRoles.Nurse => [View],
        _                 => []
    };

    public const string View   = "inventory.view";
    public const string Manage = "inventory.manage";
}
