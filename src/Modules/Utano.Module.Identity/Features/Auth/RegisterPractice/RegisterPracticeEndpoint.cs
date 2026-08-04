using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;
using Utano.Module.Identity.Features.Auth.CreatePractice;

namespace Utano.Module.Identity.Features.Auth.RegisterPractice;

// Public self-service signup - the API-key-gated /api/auth/setup stays for manual/internal use.
// Both reuse the exact same CreatePracticeCommand/handler, so there's one source of truth for
// "what happens when a practice is created" (trial start, role seeding, feature seeding) - this
// is purely a different, unauthenticated door into the same flow.
[ApiController]
[Route("api/auth")]
public class RegisterPracticeEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("signup")]
    [ProducesResponseType(typeof(CreatePracticeResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Self-service sign up - creates a new practice and its first Admin user")]
    [Tags("Identity Module")]
    public async Task<IActionResult> Register(
        [FromBody] CreatePracticeCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Register), response);
    }
}
