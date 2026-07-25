namespace Utano.Module.Core.Modules;

public static class SystemRoles
{
    public const string Admin        = "Admin";
    public const string Doctor       = "Doctor";
    public const string Nurse        = "Nurse";
    public const string Receptionist = "Receptionist";
    public const string Billing      = "Billing";

    public static readonly IReadOnlyList<string> All =
        [Admin, Doctor, Nurse, Receptionist, Billing];
}
