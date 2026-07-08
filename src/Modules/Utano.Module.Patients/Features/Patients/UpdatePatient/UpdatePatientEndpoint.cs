using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Patients.Features.Patients.UpdatePatient;

[ApiController]
[Authorize]
[Route("api/patients")]
public class UpdatePatientEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Update patient details")]
    [EndpointDescription("Updates the patient's name and notes. To manage contacts or addresses use the dedicated endpoints.")]
    [Tags("Patients Module")]
    public async Task<IActionResult> UpdatePatient(
        Guid id,
        [FromBody] UpdatePatientCommand command,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(command with { Id = id }, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}