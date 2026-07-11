using MediatR;
using Utano.Module.Core.Models;

namespace Utano.Module.ClinicalNotes.Features.Visits.GetVisits;

public record GetVisitsQuery(Guid? PatientId, Guid? DoctorId, DateOnly? Date, int Page, int PageSize) : IRequest<PagedResult<VisitSummaryResponse>>;

public record VisitSummaryResponse(Guid Id, Guid PatientId, string PatientName, Guid DoctorId, string DoctorName, DateOnly VisitDate, string? Diagnosis, string Status, DateTimeOffset CreatedAt);
