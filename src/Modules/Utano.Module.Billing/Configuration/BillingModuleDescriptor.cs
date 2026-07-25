using Utano.Module.Core.Modules;

namespace Utano.Module.Billing.Configuration;

internal sealed class BillingModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "billing";
    public string Plan => "professional";

    public IReadOnlyList<string> AllPermissions => [View, Manage];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin        => [View, Manage],
        SystemRoles.Doctor       => [View],
        SystemRoles.Billing      => [View, Manage],
        SystemRoles.Receptionist => [View],
        _                        => []
    };

    public const string View   = "billing.view";
    public const string Manage = "billing.manage";
}
