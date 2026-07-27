using Utano.Module.Core.Modules;

namespace Utano.Module.Patients.Configuration;

internal sealed class PatientsModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [View, Create, Edit, Delete, Refer];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin        => [View, Create, Edit, Delete, Refer],
        SystemRoles.Doctor       => [View, Refer],
        SystemRoles.Nurse        => [View, Refer],
        SystemRoles.Receptionist => [View, Create],
        SystemRoles.Billing      => [View],
        SystemRoles.Triage       => [View, Create],
        _                        => []
    };

    public const string View   = "patients.view";
    public const string Create = "patients.create";
    public const string Edit   = "patients.edit";
    public const string Delete = "patients.delete";
    public const string Refer  = "patients.refer";
}
