using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Enums;
using Utano.Module.Patients.Domain.ValueObjects;
using Utano.Module.Patients.Domain.Entities;

namespace Utano.Module.Patients.Domain.Entities;

public class Patient : AggregateRoot
{
    private Patient() { }

    private readonly List<PatientContact> _contacts = new();
    private readonly List<PatientAddress> _addresses = new();

    public FullName FullName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }

    // Stored as two plain columns rather than an EF Core owned type - a composite unique index
    // spanning an owner property (PracticeId) and an owned-type property isn't expressible via
    // EF Core's HasIndex (neither the lambda nor the dotted-string-path form resolves through an
    // owned navigation). Identifier re-validates on every read via PatientIdentifier.Create, which
    // is cheap and keeps the domain-facing API a single VO despite the two-column storage.
    public PatientIdentifierType IdentifierType { get; private set; }
    public string? IdentifierValue { get; private set; }
    public PatientIdentifier Identifier => PatientIdentifier.Create(IdentifierType, IdentifierValue);

    public PatientStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? Occupation { get; private set; }
    public BloodGroup? BloodGroup { get; private set; }
    public string? Allergies { get; private set; }
    public string? ChronicConditions { get; private set; }
    public Guid? MedicalAidId { get; private set; }
    public string? MedicalAidNumber { get; private set; }

    public IReadOnlyCollection<PatientContact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<PatientAddress> Addresses => _addresses.AsReadOnly();

    public static Patient Register(
        Guid practiceId,
        FullName fullName,
        DateOnly dateOfBirth,
        Gender gender,
        PatientIdentifier identifier)
    {
        if (practiceId == Guid.Empty)
            throw new UtanoDomainException("Practice is required.");

        if (dateOfBirth >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new UtanoDomainException("Date of birth must be in the past.");

        return new Patient
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            FullName = fullName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            IdentifierType = identifier.Type,
            IdentifierValue = identifier.Value,
            Status = PatientStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateIdentifier(PatientIdentifier identifier)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot update an inactive patient.");

        IdentifierType = identifier.Type;
        IdentifierValue = identifier.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(FullName fullName, string? notes = null, string? occupation = null)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot update an inactive patient.");

        FullName = fullName;
        Notes = notes;
        Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetOccupation(string? occupation)
    {
        Occupation = string.IsNullOrWhiteSpace(occupation) ? null : occupation.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateMedicalAid(Guid? medicalAidId, string? number)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot update an inactive patient.");

        MedicalAidId = medicalAidId;
        MedicalAidNumber = string.IsNullOrWhiteSpace(number) ? null : number.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateMedicalHistory(BloodGroup? bloodGroup, string? allergies, string? chronicConditions)
    {
        BloodGroup = bloodGroup;
        Allergies = allergies;
        ChronicConditions = chronicConditions;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddContact(ContactType type, string phoneNumber, string? email, bool isPrimary)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot add contact to an inactive patient.");

        if (isPrimary)
            foreach (var c in _contacts)
                c.SetPrimary(false);

        _contacts.Add(PatientContact.Create(Id, type, phoneNumber, email, isPrimary));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddAddress(AddressType type, string street, string city, string country,
        string? suburb, bool isPrimary)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot add address to an inactive patient.");

        if (isPrimary)
            foreach (var a in _addresses)
                a.SetPrimary(false);

        _addresses.Add(PatientAddress.Create(Id, type, street, city, country, suburb, isPrimary));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Patient is already inactive.");

        Status = PatientStatus.Inactive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (Status == PatientStatus.Active)
            throw new UtanoDomainException("Patient is already active.");

        Status = PatientStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
