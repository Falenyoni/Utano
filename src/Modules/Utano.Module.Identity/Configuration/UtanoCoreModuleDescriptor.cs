using Utano.Module.Core.Modules;
using Utano.Module.Identity.Domain.Constants;

namespace Utano.Module.Identity.Configuration;

// Temporary: holds all permission defaults until Phase 2 splits them per module.
internal sealed class UtanoCoreModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        "Admin"        => Permissions.AdminPermissions,
        "Doctor"       => Permissions.DoctorPermissions,
        "Nurse"        => Permissions.NursePermissions,
        "Receptionist" => Permissions.ReceptionistPermissions,
        "Billing"      => Permissions.BillingPermissions,
        _              => []
    };
}
