using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Interfaces;
using Utano.Module.Identity.Features;

namespace Utano.Module.Identity.Features.Users.ResetUserPassword;

[ApiController]
[Route("api/users")]
[Authorize]
public class ResetUserPasswordEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("{id:guid}/reset-password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Admin resets a staff member's password")]
    [Tags("Identity Module")]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetUserPasswordBody body,
        CancellationToken ct)
    {
        var ok = await sender.Send(new ResetUserPasswordCommand(id, body.NewPassword), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record ResetUserPasswordBody(string NewPassword);
public record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>;

public class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy();
    }
}

public class ResetUserPasswordHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPasswordService passwordService,
    ICurrentUserService currentUser,
    IValidator<ResetUserPasswordCommand> validator)
    : IRequestHandler<ResetUserPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetUserPasswordCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var user = await readRepository.GetByIdAsync(cmd.UserId, ct);
        if (user is null || user.PracticeId != currentUser.PracticeId)
            return false;

        user.UpdatePassword(passwordService.Hash(cmd.NewPassword));
        await writeRepository.UpdateAsync(user, ct);
        return true;
    }
}