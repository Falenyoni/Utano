using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Patients.Features.Patients.DeactivatePatient;

[ApiController]
[Route("api/patients")]
public class DeactivatePatientEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Deactivate a patient")]
    [EndpointDescription("Marks the patient as inactive. An inactive patient cannot be updated or have new contacts and addresses added.")]
    [Tags("Patients Module")]
    public async Task<IActionResult> DeactivatePatient(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivatePatientCommand(id), cancellationToken);
        return result ? NoContent() : NotFound();
    }
}