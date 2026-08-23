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

namespace Utano.Module.Identity.Features.Auth.ResendVerification;

[ApiController]
[Route("api/auth")]
public class ResendVerificationEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("resend-verification")]
    [EnableRateLimiting("resend-verification")]
    [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
    [EndpointSummary("Resend the email verification link - always returns 200 regardless of whether the email is registered or already verified")]
    [Tags("Identity Module")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "If that email is registered and not yet verified, a new verification link has been sent." });
    }
}

public record ResendVerificationCommand(string Email) : IRequest;

public class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ResendVerificationHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IEmailSender emailSender,
    IOptions<EmailVerificationSettings> settings,
    IValidator<ResendVerificationCommand> validator,
    ILogger<ResendVerificationHandler> logger)
    : IRequestHandler<ResendVerificationCommand>
{
    public async Task Handle(ResendVerificationCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid) return; // malformed email - nothing to do, still no error to the caller

        var user = await readRepository.GetByEmailAsync(command.Email, ct);
        if (user is null || user.IsEmailVerified) return; // deliberately silent either way - enumeration prevention

        var rawToken = EmailVerificationTokenHasher.GenerateToken();
        var tokenHash = EmailVerificationTokenHasher.Hash(rawToken);
        await writeRepository.AddEmailVerificationTokenAsync(user.Id, tokenHash, settings.Value.ExpiryMinutes, ct);

        var verifyUrl = $"{settings.Value.FrontendBaseUrl}/verify-email?token={rawToken}";
        var html = $"""
            <p>Hi {user.FirstName},</p>
            <p>Click the link below to verify your email and activate your Utano trial.</p>
            <p><a href="{verifyUrl}">Verify your email</a></p>
            <p>This link expires in {settings.Value.ExpiryMinutes / 60} hours.</p>
            """;

        try
        {
            await emailSender.SendAsync(user.Email.Value, "Verify your email to activate your Utano trial", html, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email to user {UserId}", user.Id);
        }
    }
}
