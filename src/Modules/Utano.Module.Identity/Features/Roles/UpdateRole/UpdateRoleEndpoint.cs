using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Identity.Features.Roles.UpdateRole;

[ApiController]
[Route("api/roles/{id:guid}")]
[Authorize]
public class UpdateRoleEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Update a role's name, description and permissions")]
    [Tags("Identity Module")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command with { Id = id }, cancellationToken);
        return NoContent();
    }
}