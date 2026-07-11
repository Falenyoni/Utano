using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.ClinicalNotes.Features.Visits.UpdateVisit;

[ApiController]
[Route("api/visits")]
[Authorize]
public class UpdateVisitEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Update clinical notes (use PUT /triage for vitals)")]
    [Tags("Clinical Notes Module")]
    public async Task<IActionResult> UpdateVisit(Guid id, [FromBody] UpdateVisitCommand command, CancellationToken cancellationToken)
    {
        var updated = await sender.Send(command with { Id = id }, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}