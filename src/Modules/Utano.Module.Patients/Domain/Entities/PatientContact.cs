using Utano.Module.Core.Exceptions;

namespace Utano.Module.Patients.Domain.Entities;

public class PatientContact
{
    private PatientContact() { }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public string Type { get; private set; }        // e.g. "Mobile", "Work", "Emergency"
    public string PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public bool IsPrimary { get; private set; }

    internal static PatientContact Create(Guid patientId, string type, string phoneNumber,
        string? email, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new UtanoDomainException("Phone number is required.");

        if (string.IsNullOrWhiteSpace(type))
            throw new UtanoDomainException("Contact type is required.");

        return new PatientContact
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Type = type.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Email = email?.Trim(),
            IsPrimary = isPrimary
        };
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
