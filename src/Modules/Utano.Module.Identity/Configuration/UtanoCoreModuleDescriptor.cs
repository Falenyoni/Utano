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
        SettingsPractice, SettingsBranding, SettingsMedicalAids, SettingsSubscription,
    ];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin => [
            SettingsUsersView, SettingsUsersManage,
            SettingsRoles,
            SettingsStaffView, SettingsStaffManage,
            SettingsPractice, SettingsBranding, SettingsMedicalAids, SettingsSubscription,
        ],
        _ => []
    };

    public const string SettingsUsersView   = "settings.users.view";
    public const string SettingsUsersManage = "settings.users.manage";
    public const string SettingsRoles       = "settings.roles";
    public const string SettingsStaffView   = "settings.staff.view";
    public const string SettingsStaffManage = "settings.staff.manage";

    // Previously one combined "settings.practice" gated all four of these tabs, with no way to
    // grant one without the others. Split 2026-08-03 so Subscription (financially sensitive) and
    // Medical Aid Schemes can be granted independently of Practice details/Branding. Existing
    // roles get all four via a one-time additive migration so nobody's access changes on upgrade.
    public const string SettingsPractice     = "settings.practice";
    public const string SettingsBranding     = "settings.branding";
    public const string SettingsMedicalAids  = "settings.medical_aids";
    public const string SettingsSubscription = "settings.subscription";
}
