using Utano.Module.Core.Modules;

namespace Utano.Module.Billing.Configuration;

internal sealed class BillingModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "billing";
    public string Plan => "professional";

    public IReadOnlyList<string> AllPermissions => [View, Manage, ClaimsView, ClaimsManage];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin        => [View, Manage, ClaimsView, ClaimsManage],
        SystemRoles.Doctor       => [View, ClaimsView],
        SystemRoles.Billing      => [View, Manage, ClaimsView, ClaimsManage],
        SystemRoles.Receptionist => [View, ClaimsView],
        _                        => []
    };

    public const string View         = "billing.view";
    public const string Manage       = "billing.manage";
    public const string ClaimsView   = "claims.view";
    public const string ClaimsManage = "claims.manage";
}
