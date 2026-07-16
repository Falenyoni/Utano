using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.PracticeSettings;

[ApiController]
[Route("api/practice")]
[Authorize]
public class PracticeEndpoints(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PracticeResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Get the current practice details")]
    [Tags("Identity Module")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetPracticeQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update the current practice details")]
    [Tags("Identity Module")]
    public async Task<IActionResult> Update([FromBody] UpdatePracticeBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new UpdatePracticeCommand(body.Name, body.ContactEmail, body.ContactPhone, body.PhysicalAddress, body.HasDispensary), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record PracticeResponse(Guid Id, string Name, string ContactEmail, string ContactPhone, string PhysicalAddress, bool HasDispensary);
public record UpdatePracticeBody(string Name, string ContactEmail, string ContactPhone, string PhysicalAddress, bool HasDispensary);

// ─── Get ────────────────────────────────────────────────────────────────────

public record GetPracticeQuery : IRequest<PracticeResponse?>;

public class GetPracticeHandler(IPracticeRepository repository, ICurrentUserService currentUser)
    : IRequestHandler<GetPracticeQuery, PracticeResponse?>
{
    public async Task<PracticeResponse?> Handle(GetPracticeQuery _, CancellationToken ct)
    {
        var practice = await repository.GetByIdAsync(currentUser.PracticeId, ct);
        if (practice is null) return null;
        return new PracticeResponse(practice.Id, practice.Name, practice.ContactEmail, practice.ContactPhone, practice.PhysicalAddress, practice.HasDispensary);
    }
}

// ─── Update ─────────────────────────────────────────────────────────────────

public record UpdatePracticeCommand(string Name, string ContactEmail, string ContactPhone, string PhysicalAddress, bool HasDispensary) : IRequest<bool>;

public class UpdatePracticeHandler(IPracticeRepository repository, ICurrentUserService currentUser)
    : IRequestHandler<UpdatePracticeCommand, bool>
{
    public async Task<bool> Handle(UpdatePracticeCommand cmd, CancellationToken ct)
    {
        var practice = await repository.GetByIdAsync(currentUser.PracticeId, ct);
        if (practice is null) return false;

        practice.Update(cmd.Name, cmd.ContactEmail, cmd.ContactPhone, cmd.PhysicalAddress);
        practice.SetHasDispensary(cmd.HasDispensary);
        await repository.UpdateAsync(practice, ct);
        return true;
    }
}