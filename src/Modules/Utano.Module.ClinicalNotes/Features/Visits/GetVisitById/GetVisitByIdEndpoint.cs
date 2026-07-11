using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.ClinicalNotes.Features.Visits.GetVisitById;

[ApiController]
[Route("api/visits")]
[Authorize]
public class GetVisitByIdEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VisitDetailResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get visit by ID")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> GetVisitById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetVisitByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}