using Utano.Module.Core.Modules;

namespace Utano.Module.ClinicalNotes.Configuration;

internal sealed class ClinicalNotesModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [View, Create, Edit, DispensaryView, DispensaryManage];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin  => [View, Create, Edit, DispensaryView, DispensaryManage],
        SystemRoles.Doctor => [View, Create, Edit, DispensaryView],
        SystemRoles.Nurse  => [View, Create, DispensaryView, DispensaryManage],
        _                  => []
    };

    public const string View             = "clinical_notes.view";
    public const string Create           = "clinical_notes.create";
    public const string Edit             = "clinical_notes.edit";
    public const string DispensaryView   = "dispensary.view";
    public const string DispensaryManage = "dispensary.manage";
}
