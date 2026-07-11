using Utano.Module.ClinicalNotes.Domain.Enums;
using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.ClinicalNotes.Domain.Entities;

public class Visit : AggregateRoot
{
    private Visit() { }

    public Guid PatientId { get; private set; }
    public string PatientName { get; private set; } = null!;
    public Guid DoctorId { get; private set; }
    public string DoctorName { get; private set; } = null!;
    public DateOnly VisitDate { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public string? PatientGender { get; private set; }
    public DateOnly? PatientDateOfBirth { get; private set; }

    // Vitals
    public int? BloodPressureSystolic { get; private set; }
    public int? BloodPressureDiastolic { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? TemperatureCelsius { get; private set; }
    public int? PulseRate { get; private set; }
    public decimal? OxygenSaturation { get; private set; }

    // Clinical
    public string? Department { get; private set; }
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
        string? patientGender = null,
        DateOnly? patientDateOfBirth = null)
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
            PatientGender = patientGender,
            PatientDateOfBirth = patientDateOfBirth,
            Status = VisitStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateVitals(
        int? bpSystolic, int? bpDiastolic,
        decimal? weightKg, decimal? heightCm,
        decimal? temperatureCelsius, int? pulseRate,
        decimal? oxygenSaturation)
    {
        BloodPressureSystolic = bpSystolic;
        BloodPressureDiastolic = bpDiastolic;
        WeightKg = weightKg;
        HeightCm = heightCm;
        TemperatureCelsius = temperatureCelsius;
        PulseRate = pulseRate;
        OxygenSaturation = oxygenSaturation;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateClinicalNotes(
        string? chiefComplaint, string? symptoms,
        string? diagnosis, string? treatment,
        string? prescription, string? notes,
        string? department = null)
    {
        ChiefComplaint = chiefComplaint;
        Symptoms = symptoms;
        Diagnosis = diagnosis;
        Treatment = treatment;
        Prescription = prescription;
        Notes = notes;
        Department = department?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Triage(
        int? bpSystolic, int? bpDiastolic,
        decimal? weightKg, decimal? heightCm,
        decimal? temperatureCelsius, int? pulseRate,
        decimal? oxygenSaturation,
        string? chiefComplaint)
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

        if (Status == VisitStatus.InProgress)
            Status = VisitStatus.Triaged;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status == VisitStatus.Completed)
            throw new UtanoDomainException("Visit is already completed.");
        Status = VisitStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
