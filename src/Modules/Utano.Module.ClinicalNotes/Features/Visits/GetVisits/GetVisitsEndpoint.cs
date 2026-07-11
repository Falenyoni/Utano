using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Models;

namespace Utano.Module.ClinicalNotes.Features.Visits.GetVisits;

[ApiController]
[Route("api/visits")]
[Authorize]
public class GetVisitsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VisitSummaryResponse>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List visits")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> GetVisits(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetVisitsQuery(patientId, doctorId, date, page, pageSize), cancellationToken);
        return Ok(result);
    }
}