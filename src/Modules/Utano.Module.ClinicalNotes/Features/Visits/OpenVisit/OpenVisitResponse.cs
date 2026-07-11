namespace Utano.Module.ClinicalNotes.Features.Visits.OpenVisit;

public record OpenVisitResponse(Guid Id, Guid PatientId, string PatientName, Guid DoctorId, string DoctorName, DateOnly VisitDate, string? Department, string Status, DateTimeOffset CreatedAt);
