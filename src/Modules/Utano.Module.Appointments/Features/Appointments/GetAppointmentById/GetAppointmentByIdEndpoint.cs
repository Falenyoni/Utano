using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointmentById;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class GetAppointmentByIdEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetAppointmentByIdResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get appointment by ID")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> GetAppointmentById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAppointmentByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
