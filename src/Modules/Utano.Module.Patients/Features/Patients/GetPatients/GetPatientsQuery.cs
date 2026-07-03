using MediatR;
using Utano.Module.Core.Models;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.GetPatients;

public record GetPatientsQuery(
    string? SearchTerm,
    PatientStatus? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<GetPatientsResponse>>;
