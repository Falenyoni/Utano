using MediatR;
using Utano.Module.Core.Models;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.GetPatients;

public class GetPatientsHandler(IPatientReadRepository readRepository)
    : IRequestHandler<GetPatientsQuery, PagedResult<GetPatientsResponse>>
{
    public async Task<PagedResult<GetPatientsResponse>> Handle(
        GetPatientsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await readRepository.GetPagedAsync(
            query.SearchTerm, query.Status, query.Page, query.PageSize, cancellationToken);

        return new PagedResult<GetPatientsResponse>
        {
            Data = result.Data.Select(p => new GetPatientsResponse(
                p.Id,
                p.FullName.Display,
                p.NationalId.Value,
                p.DateOfBirth,
                p.Gender.ToString(),
                p.Status.ToString())),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }
}
