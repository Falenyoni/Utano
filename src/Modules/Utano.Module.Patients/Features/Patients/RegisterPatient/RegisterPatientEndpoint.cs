using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Patients.Features.Patients.RegisterPatient;

[ApiController]
[Route("api/patients")]
public class RegisterPatientEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(RegisterPatientResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> RegisterPatient(
        RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(RegisterPatient), new { id = response.Id }, response);
    }
}