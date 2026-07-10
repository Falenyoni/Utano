using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Users.GetUsers;

[ApiController]
[Route("api/users")]
[Authorize]
public class GetUsersEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserSummaryResponse>), (int)HttpStatusCode.OK)]
    [EndpointSummary("List all staff users in this practice")]
    [Tags("Identity Module")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? role,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUsersQuery(role), cancellationToken);
        return Ok(result);
    }
}
