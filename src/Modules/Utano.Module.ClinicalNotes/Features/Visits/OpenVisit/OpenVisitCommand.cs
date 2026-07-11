using MediatR;

namespace Utano.Module.ClinicalNotes.Features.Visits.OpenVisit;

public record OpenVisitCommand(
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateOnly VisitDate,
    Guid? AppointmentId = null,
    string? Department = null
) : IRequest<OpenVisitResponse>;
