using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Appointments.Features.Appointments.CancelAppointment;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class CancelAppointmentEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/cancel")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Cancel an appointment")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> CancelAppointment(
        Guid id,
        [FromBody] CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CancelAppointmentCommand(id, request.Reason), cancellationToken);
        return NoContent();
    }
}

public record CancelAppointmentRequest(string Reason);
