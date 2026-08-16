using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Configuration;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.ForgotPassword;

[ApiController]
[Route("api/auth")]
public class ForgotPasswordEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
    [EndpointSummary("Request a password reset email - always returns 200 regardless of whether the email is registered")]
    [Tags("Identity Module")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "If that email is registered, a reset link has been sent." });
    }
}

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IEmailSender emailSender,
    IOptions<PasswordResetSettings> settings,
    IValidator<ForgotPasswordCommand> validator,
    ILogger<ForgotPasswordHandler> logger)
    : IRequestHandler<ForgotPasswordCommand>
{
    public async Task Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid) return; // malformed email - nothing to do, still no error to the caller

        var user = await readRepository.GetByEmailAsync(command.Email, ct);
        if (user is null) return; // deliberately silent - this endpoint must not reveal whether an email is registered

        var rawToken = PasswordResetTokenHasher.GenerateToken();
        var tokenHash = PasswordResetTokenHasher.Hash(rawToken);
        await writeRepository.AddPasswordResetTokenAsync(user.Id, tokenHash, settings.Value.ExpiryMinutes, ct);

        var resetUrl = $"{settings.Value.FrontendBaseUrl}/reset-password?token={rawToken}";
        var html = $"""
            <p>Hi {user.FirstName},</p>
            <p>Click the link below to reset your Utano password. This link expires in {settings.Value.ExpiryMinutes} minutes.</p>
            <p><a href="{resetUrl}">Reset your password</a></p>
            <p>If you didn't request this, you can safely ignore this email - your password won't change.</p>
            """;

        try
        {
            await emailSender.SendAsync(user.Email.Value, "Reset your Utano password", html, ct);
        }
        catch (Exception ex)
        {
            // Swallowed deliberately - a delivery failure must not surface to the caller either,
            // for the same enumeration-prevention reason as the "user not found" case above.
            logger.LogError(ex, "Failed to send password reset email to user {UserId}", user.Id);
        }
    }
}
