using Utano.Module.Core.Modules;

namespace Utano.Module.Identity.Configuration;

// Owns permissions that have no dedicated module yet: user management and reports.
internal sealed class UtanoCoreModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions =>
    [
        SettingsUsersView, SettingsUsersManage,
        SettingsRoles,
        SettingsStaffView, SettingsStaffManage,
        SettingsPractice,
        ReportsView,
    ];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin => [
            SettingsUsersView, SettingsUsersManage,
            SettingsRoles,
            SettingsStaffView, SettingsStaffManage,
            SettingsPractice,
            ReportsView,
        ],
        SystemRoles.Doctor  => [ReportsView],
        SystemRoles.Billing => [ReportsView],
        _                   => []
    };

    public const string SettingsUsersView   = "settings.users.view";
    public const string SettingsUsersManage = "settings.users.manage";
    public const string SettingsRoles       = "settings.roles";
    public const string SettingsStaffView   = "settings.staff.view";
    public const string SettingsStaffManage = "settings.staff.manage";
    public const string SettingsPractice    = "settings.practice";
    public const string ReportsView         = "reports.view";
}
