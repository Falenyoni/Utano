using Utano.Module.Core.Modules;

namespace Utano.Module.Identity.Configuration;

// Owns permissions that have no dedicated module yet: user management and reports.
internal sealed class UtanoCoreModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [SettingsUsers, ReportsView];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin   => [SettingsUsers, ReportsView],
        SystemRoles.Doctor  => [ReportsView],
        SystemRoles.Billing => [ReportsView],
        _                   => []
    };

    public const string SettingsUsers = "settings.users";
    public const string ReportsView   = "reports.view";
}
