using MediatR;

namespace Utano.Module.ClinicalNotes.Features.Visits.UpdateVisit;

public record UpdateVisitCommand(
    Guid Id,
    string? ChiefComplaint,
    string? Symptoms,
    string? Diagnosis,
    string? Treatment,
    string? Prescription,
    string? Notes,
    string? Department = null
) : IRequest<bool>;
