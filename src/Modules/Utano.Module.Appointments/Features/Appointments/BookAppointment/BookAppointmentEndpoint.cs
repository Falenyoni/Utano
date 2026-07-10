using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Appointments.Features.Appointments.BookAppointment;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class BookAppointmentEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(BookAppointmentResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Book an appointment")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> BookAppointment(
        [FromBody] BookAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(BookAppointment), new { id = response.Id }, response);
    }
}
