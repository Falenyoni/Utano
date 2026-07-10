using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Users.GetDoctors;

[ApiController]
[Route("api/users")]
[Authorize]
public class GetDoctorsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet("doctors")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorResponse>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Get all doctors in this practice")]
    [Tags("Identity Module")]
    public async Task<IActionResult> GetDoctors(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDoctorsQuery(), cancellationToken);
        return Ok(result);
    }
}
