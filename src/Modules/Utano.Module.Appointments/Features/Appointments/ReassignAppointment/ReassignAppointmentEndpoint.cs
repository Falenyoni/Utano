using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Utano.Module.Appointments.Features.Appointments.ReassignAppointment;

public record ReassignAppointmentCommand(Guid Id, Guid NewDoctorId, string NewDoctorName) : IRequest;

public record ReassignAppointmentBody(Guid NewDoctorId, string NewDoctorName);

[ApiController]
[Route("api/appointments/{id:guid}")]
[Authorize]
public class ReassignAppointmentEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("reassign")]
    [ProducesResponseType(204)]
    [Tags("Appointments Module")]
    public async Task<IActionResult> Reassign(
        [FromRoute] Guid id,
        [FromBody] ReassignAppointmentBody body,
        CancellationToken ct)
    {
        await sender.Send(new ReassignAppointmentCommand(id, body.NewDoctorId, body.NewDoctorName), ct);
        return NoContent();
    }
}
