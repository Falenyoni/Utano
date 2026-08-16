using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.ChangePassword;

[ApiController]
[Route("api/auth")]
[Authorize]
public class ChangePasswordEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Change the currently authenticated user's password")]
    [Tags("Identity Module")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordBody body, CancellationToken ct)
    {
        await sender.Send(new ChangePasswordCommand(body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }
}

public record ChangePasswordBody(string CurrentPassword, string NewPassword);
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy();
    }
}

public class ChangePasswordHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPasswordService passwordService,
    ICurrentUserService currentUser,
    IEmailSender emailSender,
    IValidator<ChangePasswordCommand> validator,
    ILogger<ChangePasswordHandler> logger)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var user = await readRepository.GetByIdAsync(currentUser.UserId, ct)
            ?? throw new UtanoDomainException("User not found.");

        if (!passwordService.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UtanoDomainException("Current password is incorrect.");

        user.UpdatePassword(passwordService.Hash(cmd.NewPassword));
        await writeRepository.UpdateAsync(user, ct);

        // Security notification, not a verification step - the change has already happened by the
        // time this sends, so a delivery failure must never block or roll back the password
        // change itself.
        try
        {
            await emailSender.SendAsync(
                user.Email.Value,
                "Your Utano password was changed",
                $"""
                <p>Hi {user.FirstName},</p>
                <p>Your Utano password was just changed.</p>
                <p>If this wasn't you, contact your practice administrator immediately.</p>
                """,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password-change notification to user {UserId}", user.Id);
        }
    }
}