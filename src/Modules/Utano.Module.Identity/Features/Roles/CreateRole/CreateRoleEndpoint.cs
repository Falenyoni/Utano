using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Utano.Module.Identity.Features.Roles.CreateRole;

[ApiController]
[Route("api/roles")]
[Authorize]
public class CreateRoleEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Create a new role")]
    [Tags("Identity Module")]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateRole), new { id }, id);
    }
}