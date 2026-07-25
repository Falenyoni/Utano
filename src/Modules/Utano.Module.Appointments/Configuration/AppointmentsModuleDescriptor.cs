using Utano.Module.Core.Modules;

namespace Utano.Module.Appointments.Configuration;

internal sealed class AppointmentsModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [View, Create, Edit, Cancel];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin        => [View, Create, Edit, Cancel],
        SystemRoles.Doctor       => [View, Create, Edit],
        SystemRoles.Nurse        => [View, Create],
        SystemRoles.Receptionist => [View, Create, Edit, Cancel],
        SystemRoles.Billing      => [View],
        _                        => []
    };

    public const string View   = "appointments.view";
    public const string Create = "appointments.create";
    public const string Edit   = "appointments.edit";
    public const string Cancel = "appointments.cancel";
}
