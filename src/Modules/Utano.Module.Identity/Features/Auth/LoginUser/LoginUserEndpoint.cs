using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Auth.LoginUser;

[ApiController]
[Route("api/auth")]
public class LoginUserEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginUserResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Login")]
    [EndpointDescription("Authenticates a user and returns a JWT access token and refresh token.")]
    [Tags("Identity Module")]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return Ok(response);
    }
}