using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.ClinicalNotes.Features.Visits.OpenVisit;

[ApiController]
[Route("api/visits")]
[Authorize]
public class OpenVisitEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(OpenVisitResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Open a new visit")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> OpenVisit([FromBody] OpenVisitCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(OpenVisit), new { id = response.Id }, response);
    }
}
