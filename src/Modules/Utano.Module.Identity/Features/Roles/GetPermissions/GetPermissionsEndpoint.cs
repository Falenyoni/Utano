using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Identity.Features.Roles.GetPermissions;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class GetPermissionsEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<string>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Get all available permission keys")]
    [Tags("Identity Module")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPermissionsQuery(), cancellationToken);
        return Ok(result);
    }
}