using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using Utano.Module.Appointments.Domain.Enums;
using Utano.Module.Core.Models;

namespace Utano.Module.Appointments.Features.Appointments.GetAppointments;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class GetAppointmentsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AppointmentSummaryResponse>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List appointments")]
    [Tags("Appointments Module")]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAppointmentsQuery(date, patientId, doctorId, status, page, pageSize),
            cancellationToken);
        return Ok(result);
    }
}
