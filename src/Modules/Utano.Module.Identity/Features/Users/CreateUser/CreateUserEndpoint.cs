using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Users.CreateUser;

[ApiController]
[Route("api/users")]
[Authorize]
public class CreateUserEndpoint(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Create a new staff user (doctor, receptionist, billing, admin)")]
    [Tags("Identity Module")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreateUser), new { id = response.Id }, response);
    }
}
