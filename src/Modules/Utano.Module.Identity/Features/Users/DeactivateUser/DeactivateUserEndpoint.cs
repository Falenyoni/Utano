using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Users.DeactivateUser;

[ApiController]
[Route("api/users")]
[Authorize]
public class DeactivateUserEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}/deactivate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Deactivate a staff user")]
    [Tags("Identity Module")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }
}
