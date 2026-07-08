using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Patients.Features.Patients.RegisterPatient;

[ApiController]
[Route("api/patients")]
[Authorize]
public class RegisterPatientEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RegisterPatientResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Register a new patient")]
    [EndpointDescription("Registers a new patient and returns the patient details.")]
    [Tags("Patients Module")]
    public async Task<IActionResult> RegisterPatient(
        [FromBody] RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(RegisterPatient), new { id = response.Id }, response);
    }
}