using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Appointments.Features.Appointments.UndoNoShowAppointment;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class UndoNoShowAppointmentEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/undo-no-show")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Revert a wrongly-marked no-show back to Checked In")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> UndoNoShow(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new UndoNoShowAppointmentCommand(id), cancellationToken);
        return NoContent();
    }
}
