using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;
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
        var ok = await sender.Send(new UpdateUserCommand(id, body.FirstName, body.LastName, body.Role, body.Specialty), ct);
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

public record UpdateUserBody(string FirstName, string LastName, string Role, string? Specialty = null);

public record UpdateUserCommand(Guid Id, string FirstName, string LastName, string Role, string? Specialty = null) : IRequest<bool>;

public class UpdateUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    ICurrentUserService currentUser,
    IAuditService auditService,
    ILogger<UpdateUserHandler> logger)
    : IRequestHandler<UpdateUserCommand, bool>
{
    public async Task<bool> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await readRepository.GetByIdAsync(cmd.Id, ct);
        if (user is null || user.PracticeId != currentUser.PracticeId) return false;

        if (!SystemRoles.All.Contains(cmd.Role, StringComparer.OrdinalIgnoreCase))
            throw new UtanoDomainException($"Invalid role: {cmd.Role}");

        user.Update(cmd.FirstName, cmd.LastName, cmd.Role, cmd.Specialty);
        await writeRepository.UpdateAsync(user, ct);

        try
        {
            await auditService.LogAsync("User", user.Id.ToString(), "Updated",
                $"Name: {user.FullName} · Role: {user.Role}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log user update for {UserId}", user.Id);
        }

        return true;
    }
}

public record ActivateUserCommand(Guid Id) : IRequest<bool>;

public class ActivateUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    ICurrentUserService currentUser,
    IAuditService auditService,
    ILogger<ActivateUserHandler> logger)
    : IRequestHandler<ActivateUserCommand, bool>
{
    public async Task<bool> Handle(ActivateUserCommand cmd, CancellationToken ct)
    {
        var user = await readRepository.GetByIdAsync(cmd.Id, ct);
        if (user is null || user.PracticeId != currentUser.PracticeId) return false;

        user.Activate();
        await writeRepository.UpdateAsync(user, ct);

        try
        {
            await auditService.LogAsync("User", user.Id.ToString(), "Activated",
                $"Name: {user.FullName}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log user activation for {UserId}", user.Id);
        }

        return true;
    }
}
