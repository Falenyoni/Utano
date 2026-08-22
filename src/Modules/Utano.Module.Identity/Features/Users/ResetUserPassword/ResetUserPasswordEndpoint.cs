using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Core.Authorization;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Configuration;
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

public record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest<bool>, IRequirePermission
{
    public string Permission => UtanoCoreModuleDescriptor.SettingsUsersManage;
}

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
    IEmailSender emailSender,
    IAuditService auditService,
    IValidator<ResetUserPasswordCommand> validator,
    ILogger<ResetUserPasswordHandler> logger)
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

        // Notifies the affected staff member their password changed, since - unlike self-service
        // Change Password - they didn't take this action themselves. Never blocks the reset itself.
        try
        {
            await emailSender.SendAsync(
                user.Email.Value,
                "Your Utano password was reset",
                $"""
                <p>Hi {user.FirstName},</p>
                <p>An administrator at your practice just reset your Utano password.</p>
                <p>If you weren't expecting this, contact your practice administrator.</p>
                """,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password-reset notification to user {UserId}", user.Id);
        }

        try
        {
            await auditService.LogAsync("User", user.Id.ToString(), "PasswordReset",
                $"Name: {user.FullName} · Reset by an administrator", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log password reset for {UserId}", user.Id);
        }

        return true;
    }
}