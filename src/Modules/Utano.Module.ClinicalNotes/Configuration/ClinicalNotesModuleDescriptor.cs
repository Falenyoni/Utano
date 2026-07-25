using Utano.Module.Core.Modules;

namespace Utano.Module.ClinicalNotes.Configuration;

internal sealed class ClinicalNotesModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [View, Create, Edit];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin  => [View, Create, Edit],
        SystemRoles.Doctor => [View, Create, Edit],
        SystemRoles.Nurse  => [View, Create],
        _                  => []
    };

    public const string View   = "clinical_notes.view";
    public const string Create = "clinical_notes.create";
    public const string Edit   = "clinical_notes.edit";
}
