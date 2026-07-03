using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Domain.Entities;

public class PatientContact
{
    private PatientContact() { }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public ContactType Type { get; private set; }
    public string PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public bool IsPrimary { get; private set; }

    internal static PatientContact Create(Guid patientId, ContactType type, string phoneNumber,
        string? email, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new UtanoDomainException("Phone number is required.");

        return new PatientContact
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Type = type,
            PhoneNumber = phoneNumber.Trim(),
            Email = email?.Trim(),
            IsPrimary = isPrimary
        };
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
