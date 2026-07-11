using MediatR;

namespace Utano.Module.ClinicalNotes.Features.Visits.UpdateVisit;

public record UpdateVisitCommand(
    Guid Id,
    int? BloodPressureSystolic, int? BloodPressureDiastolic,
    decimal? WeightKg, decimal? HeightCm,
    decimal? TemperatureCelsius, int? PulseRate, decimal? OxygenSaturation,
    string? ChiefComplaint, string? Symptoms,
    string? Diagnosis, string? Treatment,
    string? Prescription, string? Notes,
    string? Department = null
) : IRequest<bool>;
