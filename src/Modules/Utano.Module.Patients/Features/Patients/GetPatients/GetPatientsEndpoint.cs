using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Utano.Module.Core.Models;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.GetPatients;

[ApiController]
[Route("api/patients")]
public class GetPatientsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GetPatientsResponse>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Get a paged list of patients")]
    [EndpointDescription("Returns a paginated list of patients for the current practice. Optionally filter by status or search by name and national ID.")]
    [Tags("Patients Module")]
    public async Task<IActionResult> GetPatients(
        [FromQuery] string? searchTerm,
        [FromQuery] PatientStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var response = await sender.Send(
            new GetPatientsQuery(searchTerm, status, page, pageSize), cancellationToken);
        return Ok(response);
    }
}