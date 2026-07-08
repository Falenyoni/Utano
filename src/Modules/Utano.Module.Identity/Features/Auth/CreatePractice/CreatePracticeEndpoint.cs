using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

[ApiController]
[Route("api/auth")]
public class CreatePracticeEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("setup")]
    [ProducesResponseType(typeof(CreatePracticeResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Create a new practice")]
    [EndpointDescription("Registers a new practice and creates the first Admin user. Use this once during initial setup.")]
    [Tags("Identity Module")]
    public async Task<IActionResult> CreatePractice(
        [FromBody] CreatePracticeCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(CreatePractice), response);
    }
}