using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Enums;
using Utano.Module.Patients.Domain.ValueObjects;

namespace Utano.Module.Patients.Domain.Entities;

public class Patient : AggregateRoot
{
    private Patient() { }

    private readonly List<PatientContact> _contacts = new();
    private readonly List<PatientAddress> _addresses = new();

    public FullName FullName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public NationalId NationalId { get; private set; }
    public PatientStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<PatientContact> Contacts => _contacts.AsReadOnly();
    public IReadOnlyCollection<PatientAddress> Addresses => _addresses.AsReadOnly();

    public static Patient Register(
        Guid practiceId,
        FullName fullName,
        DateOnly dateOfBirth,
        Gender gender,
        NationalId nationalId)
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
            NationalId = nationalId,
            Status = PatientStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(FullName fullName, string? notes = null)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot update an inactive patient.");

        FullName = fullName;
        Notes = notes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddContact(string type, string phoneNumber, string? email, bool isPrimary)
    {
        if (Status == PatientStatus.Inactive)
            throw new UtanoDomainException("Cannot add contact to an inactive patient.");

        if (isPrimary)
            foreach (var c in _contacts)
                c.SetPrimary(false);

        _contacts.Add(PatientContact.Create(Id, type, phoneNumber, email, isPrimary));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddAddress(string type, string street, string city, string country,
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
