using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Identity.Features.Roles.GetRoles;

[ApiController]
[Route("api/roles")]
[Authorize]
public class GetRolesEndpoint(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<RoleRow>), (int)HttpStatusCode.OK)]
    [EndpointSummary("Get all roles for the practice")]
    [Tags("Identity Module")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRolesQuery(), cancellationToken);
        return Ok(result);
    }
}