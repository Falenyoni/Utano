using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Appointments.Features.Appointments.CheckInAppointment;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class CheckInAppointmentEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/check-in")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Check in a patient for their appointment")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckInAppointmentCommand(id), cancellationToken);
        return NoContent();
    }
}
