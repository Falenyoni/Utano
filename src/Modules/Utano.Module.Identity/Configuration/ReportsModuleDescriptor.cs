using Utano.Module.Core.Modules;

namespace Utano.Module.Identity.Configuration;

internal sealed class ReportsModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "reports";
    public string Plan => "professional";

    public IReadOnlyList<string> AllPermissions => [View];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin   => [View],
        SystemRoles.Doctor  => [View],
        SystemRoles.Billing => [View],
        _                   => []
    };

    public const string View = "reports.view";
}
