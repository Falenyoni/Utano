using MediatR;

namespace Utano.Module.ClinicalNotes.Features.Visits.GetVisitById;

public record GetVisitByIdQuery(Guid Id) : IRequest<VisitDetailResponse?>;

public record VisitDetailResponse(
    Guid Id,
    Guid PatientId, string PatientName,
    Guid DoctorId, string DoctorName,
    DateOnly VisitDate,
    int? BloodPressureSystolic, int? BloodPressureDiastolic,
    decimal? WeightKg, decimal? HeightCm,
    decimal? TemperatureCelsius, int? PulseRate, decimal? OxygenSaturation,
    string? Department,
    string? ChiefComplaint, string? Symptoms,
    string? Diagnosis, string? Treatment,
    string? Prescription, string? Notes,
    string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt
);
