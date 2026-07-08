using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Auth.RefreshToken;

[ApiController]
[Route("api/auth")]
public class RefreshTokenEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Refresh access token")]
    [EndpointDescription("Exchanges a valid refresh token for a new JWT access token and refresh token.")]
    [Tags("Identity Module")]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return Ok(response);
    }
}