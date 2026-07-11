using MediatR;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.Core.Models;

namespace Utano.Module.ClinicalNotes.Features.Visits.GetVisits;

public class GetVisitsHandler(IVisitReadRepository readRepository)
    : IRequestHandler<GetVisitsQuery, PagedResult<VisitSummaryResponse>>
{
    public async Task<PagedResult<VisitSummaryResponse>> Handle(GetVisitsQuery query, CancellationToken cancellationToken)
    {
        var result = await readRepository.GetPagedAsync(query.PatientId, query.DoctorId, query.Date, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<VisitSummaryResponse>
        {
            Data = result.Data.Select(v => new VisitSummaryResponse(v.Id, v.PatientId, v.PatientName, v.DoctorId, v.DoctorName, v.VisitDate, v.Diagnosis, v.Status.ToString(), v.CreatedAt)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
