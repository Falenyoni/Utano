using Utano.Module.Core.Modules;

namespace Utano.Module.ClinicalNotes.Configuration;

internal sealed class ClinicalNotesModuleDescriptor : IModuleDescriptor
{
    public string FeatureKey => "core";
    public string Plan => "free";

    public IReadOnlyList<string> AllPermissions => [View, Create, Edit, TriageCreate, DispensaryView, DispensaryManage];

    public IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        SystemRoles.Admin   => [View, Create, Edit, TriageCreate, DispensaryView, DispensaryManage],
        SystemRoles.Doctor  => [View, Create, Edit, TriageCreate, DispensaryView],
        SystemRoles.Nurse   => [View, Create, Edit, TriageCreate, DispensaryView, DispensaryManage],
        SystemRoles.Triage  => [View, Create, TriageCreate],
        _                   => []
    };

    public const string View             = "clinical_notes.view";
    public const string Create           = "clinical_notes.create";
    public const string Edit             = "clinical_notes.edit";
    public const string TriageCreate     = "triage.create";
    public const string DispensaryView   = "dispensary.view";
    public const string DispensaryManage = "dispensary.manage";
}
