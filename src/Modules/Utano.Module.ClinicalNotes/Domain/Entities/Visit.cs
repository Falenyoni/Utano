using Utano.Module.ClinicalNotes.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Domain.Events;
using Utano.Module.Core.Domain.Events.ClinicalNotes;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.ClinicalNotes.Domain.Entities;

public class Visit : AggregateRoot, IHasDomainEvents
{
    private Visit() { }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public Guid DoctorId { get; private set; }
    public string DoctorName { get; private set; } = null!;
    public DateOnly VisitDate { get; private set; }
    public Guid? AppointmentId { get; private set; }

    // Vitals
    public int? BloodPressureSystolic { get; private set; }
    public int? BloodPressureDiastolic { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? TemperatureCelsius { get; private set; }
    public int? PulseRate { get; private set; }
    public decimal? OxygenSaturation { get; private set; }

    public int? PainScore { get; private set; }
    public string? Priority { get; private set; }

    // Clinical
    public string? Department { get; private set; }
    public string? Specialty { get; private set; }
    public string? SpecialtyData { get; private set; }
    public string? ChiefComplaint { get; private set; }
    public string? Symptoms { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Treatment { get; private set; }
    public string? Prescription { get; private set; }
    public string? Notes { get; private set; }

    public VisitStatus Status { get; private set; }

    public static Visit Open(
        Guid practiceId,
        Guid patientId,
        string patientName,
        Guid doctorId,
        string doctorName,
        DateOnly visitDate,
        Guid? appointmentId = null,
        string? department = null,
        string? specialty = null)
    {
        if (string.IsNullOrWhiteSpace(patientName))
            throw new UtanoDomainException("Patient name is required.");
        if (string.IsNullOrWhiteSpace(doctorName))
            throw new UtanoDomainException("Doctor name is required.");

        return new Visit
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            PatientId = patientId,
            PatientName = patientName,
            DoctorId = doctorId,
            DoctorName = doctorName,
            VisitDate = visitDate,
            AppointmentId = appointmentId,
            Department = department?.Trim(),
            Specialty = specialty?.Trim(),
            Status = VisitStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateClinicalNotes(
        string? chiefComplaint, string? symptoms,
        string? diagnosis, string? treatment,
        string? prescription, string? notes,
        string? department = null,
        string? specialty = null,
        string? specialtyData = null)
    {
        ChiefComplaint = chiefComplaint;
        Symptoms = symptoms;
        Diagnosis = diagnosis;
        Treatment = treatment;
        Prescription = prescription;
        Notes = notes;
        Department = department?.Trim();
        Specialty = string.IsNullOrWhiteSpace(specialty) ? Specialty : specialty.Trim();
        SpecialtyData = specialtyData;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VisitClinicalNotesUpdatedEvent(PracticeId, Id, PatientName));
    }

    public void Triage(
        int? bpSystolic, int? bpDiastolic,
        decimal? weightKg, decimal? heightCm,
        decimal? temperatureCelsius, int? pulseRate,
        decimal? oxygenSaturation,
        string? chiefComplaint,
        int? painScore = null,
        string? priority = null)
    {
        if (Status == VisitStatus.Completed)
            throw new UtanoDomainException("Cannot triage a completed visit.");

        BloodPressureSystolic = bpSystolic;
        BloodPressureDiastolic = bpDiastolic;
        WeightKg = weightKg;
        HeightCm = heightCm;
        TemperatureCelsius = temperatureCelsius;
        PulseRate = pulseRate;
        OxygenSaturation = oxygenSaturation;
        ChiefComplaint = chiefComplaint;
        PainScore = painScore;
        Priority = priority;

        if (Status == VisitStatus.InProgress)
            Status = VisitStatus.Triaged;

        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VisitTriagedEvent(PracticeId, Id, PatientName));
    }

    public void Complete()
    {
        if (Status == VisitStatus.Completed)
            throw new UtanoDomainException("Visit is already completed.");
        Status = VisitStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new VisitCompletedEvent(PracticeId, Id, PatientName, DoctorName));
    }
}
