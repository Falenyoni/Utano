using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Identity.Features.Users.AssignUserRoles;

[ApiController]
[Route("api/users/{id:guid}/roles")]
[Authorize]
public class AssignUserRolesEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Assign roles to a user (full replace)")]
    [Tags("Identity Module")]
    public async Task<IActionResult> AssignUserRoles(
        Guid id,
        [FromBody] AssignUserRolesCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command with { UserId = id }, cancellationToken);
        return NoContent();
    }
}