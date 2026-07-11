using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.UpdateUser;

[ApiController]
[Route("api/users")]
[Authorize]
public class UpdateUserEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Update a staff member's name and role")]
    [Tags("Identity Module")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserBody body, CancellationToken ct)
    {
        var ok = await sender.Send(new UpdateUserCommand(id, body.FirstName, body.LastName, body.Role), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [EndpointSummary("Reactivate a deactivated staff member")]
    [Tags("Identity Module")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var ok = await sender.Send(new ActivateUserCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record UpdateUserBody(string FirstName, string LastName, string Role);

public record UpdateUserCommand(Guid Id, string FirstName, string LastName, string Role) : IRequest<bool>;

public class UpdateUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await readRepository.GetByIdAsync(cmd.Id, ct);
        if (user is null || user.PracticeId != currentUser.PracticeId) return false;

        if (!Enum.TryParse<UserRole>(cmd.Role, ignoreCase: true, out var role))
            throw new UtanoDomainException($"Invalid role: {cmd.Role}");

        user.Update(cmd.FirstName, cmd.LastName, role);
        await writeRepository.UpdateAsync(user, ct);
        return true;
    }
}

public record ActivateUserCommand(Guid Id) : IRequest<bool>;

public class ActivateUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<ActivateUserCommand, bool>
{
    public async Task<bool> Handle(ActivateUserCommand cmd, CancellationToken ct)
    {
        var user = await readRepository.GetByIdAsync(cmd.Id, ct);
        if (user is null || user.PracticeId != currentUser.PracticeId) return false;

        user.Activate();
        await writeRepository.UpdateAsync(user, ct);
        return true;
    }
}