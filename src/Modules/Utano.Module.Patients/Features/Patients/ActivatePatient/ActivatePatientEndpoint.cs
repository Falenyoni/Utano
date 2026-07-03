using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Patients.Features.Patients.ActivatePatient;

[ApiController]
[Route("api/patients")]
public class ActivatePatientEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Activate a patient")]
    [EndpointDescription("Restores an inactive patient to active status.")]
    [Tags("Patients Module")]
    public async Task<IActionResult> ActivatePatient(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivatePatientCommand(id), cancellationToken);
        return result ? NoContent() : NotFound();
    }
}