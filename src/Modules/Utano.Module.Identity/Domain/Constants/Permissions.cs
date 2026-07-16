namespace Utano.Module.Identity.Domain.Constants;

public static class Permissions
{
    public static class Patients
    {
        public const string View   = "patients.view";
        public const string Create = "patients.create";
        public const string Edit   = "patients.edit";
        public const string Delete = "patients.delete";
    }

    public static class Appointments
    {
        public const string View   = "appointments.view";
        public const string Create = "appointments.create";
        public const string Edit   = "appointments.edit";
        public const string Cancel = "appointments.cancel";
    }

    public static class ClinicalNotes
    {
        public const string View   = "clinical_notes.view";
        public const string Create = "clinical_notes.create";
        public const string Edit   = "clinical_notes.edit";
    }

    public static class Inventory
    {
        public const string View   = "inventory.view";
        public const string Manage = "inventory.manage";
    }

    public static class Billing
    {
        public const string View   = "billing.view";
        public const string Manage = "billing.manage";
    }

    public static class Reports
    {
        public const string View = "reports.view";
    }

    public static class Settings
    {
        public const string Users = "settings.users";
    }

    public static readonly IReadOnlyList<string> All =
    [
        Patients.View, Patients.Create, Patients.Edit, Patients.Delete,
        Appointments.View, Appointments.Create, Appointments.Edit, Appointments.Cancel,
        ClinicalNotes.View, ClinicalNotes.Create, ClinicalNotes.Edit,
        Inventory.View, Inventory.Manage,
        Billing.View, Billing.Manage,
        Reports.View,
        Settings.Users,
    ];

    public static readonly IReadOnlyList<string> AdminPermissions = All;

    public static readonly IReadOnlyList<string> DoctorPermissions =
    [
        Patients.View,
        Appointments.View, Appointments.Create, Appointments.Edit,
        ClinicalNotes.View, ClinicalNotes.Create, ClinicalNotes.Edit,
        Billing.View,
        Reports.View,
    ];

    public static readonly IReadOnlyList<string> NursePermissions =
    [
        Patients.View,
        Appointments.View, Appointments.Create,
        ClinicalNotes.View, ClinicalNotes.Create,
        Inventory.View,
    ];

    public static readonly IReadOnlyList<string> ReceptionistPermissions =
    [
        Patients.View, Patients.Create,
        Appointments.View, Appointments.Create, Appointments.Edit, Appointments.Cancel,
        Billing.View,
    ];

    public static readonly IReadOnlyList<string> BillingPermissions =
    [
        Patients.View,
        Appointments.View,
        Billing.View, Billing.Manage,
        Reports.View,
    ];
}
