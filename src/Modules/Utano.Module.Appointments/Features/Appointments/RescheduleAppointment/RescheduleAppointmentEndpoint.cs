using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Appointments.Features.Appointments.RescheduleAppointment;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class RescheduleAppointmentEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/reschedule")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Reschedule an appointment")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> RescheduleAppointment(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RescheduleAppointmentCommand(id, request.NewDate, request.NewStartTime, request.NewEndTime),
            cancellationToken);
        return NoContent();
    }
}

public record RescheduleAppointmentRequest(DateOnly NewDate, TimeOnly NewStartTime, TimeOnly NewEndTime);
