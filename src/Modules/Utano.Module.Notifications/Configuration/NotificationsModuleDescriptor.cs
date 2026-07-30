using Utano.Module.Core.Modules;

namespace Utano.Module.Notifications.Configuration;

internal sealed class NotificationsModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [View];

    // Every role gets notifications.view - it only ever gates a user's own notifications,
    // never anyone else's data, so there's nothing to restrict per role.
    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => [View];

    public const string View = "notifications.view";
}
