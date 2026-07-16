using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Features.Branding.GetBranding;
using Utano.Module.Identity.Features.Branding.UpdateBranding;

namespace Utano.Module.Identity.Features.Branding;

[ApiController]
[Route("api/branding")]
[Authorize]
[Tags("Identity Module")]
public class BrandingEndpoints(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await sender.Send(new GetBrandingQuery(currentUser.PracticeId), ct));

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] UpdateBrandingRequest req, CancellationToken ct)
    {
        await sender.Send(new UpdateBrandingCommand(currentUser.PracticeId, req.PrimaryColor, req.LogoBase64), ct);
        return Ok();
    }
}

public record UpdateBrandingRequest(string? PrimaryColor, string? LogoBase64);
