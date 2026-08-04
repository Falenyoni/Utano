using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Configuration;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;
using Utano.Module.Identity.Features.Admin;

namespace Utano.Module.Identity.Features.Auth.LoginUser;

public class LoginUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPracticeRepository practiceRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings,
    IValidator<LoginUserCommand> validator,
    ISender sender)
    : IRequestHandler<LoginUserCommand, LoginUserResponse>
{
    public async Task<LoginUserResponse> Handle(
        LoginUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var user = await readRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !passwordService.Verify(command.Password, user.PasswordHash))
        {
            // Record the failed attempt even when the email doesn't exist would leak account
            // existence via timing/behavior differences either way, so only the real-user path
            // records anything - a nonexistent email already gets the same generic error with no
            // side effects either way.
            if (user is not null)
            {
                user.RecordFailedLogin();
                await writeRepository.UpdateAsync(user, cancellationToken);
            }

            throw new UtanoDomainException("Invalid email or password.");
        }

        if (user.IsLockedOut(DateTimeOffset.UtcNow))
            throw new UtanoDomainException(
                $"Too many failed login attempts. Please try again in {Math.Ceiling((user.LockedOutUntil!.Value - DateTimeOffset.UtcNow).TotalMinutes)} minute(s).");

        if (user.Status != UserStatus.Active)
            throw new UtanoDomainException("Your account is not active. Contact your administrator.");

        user.RecordSuccessfulLogin();
        await writeRepository.UpdateAsync(user, cancellationToken);

        var practice = await practiceRepository.GetByIdAsync(user.PracticeId, cancellationToken);

        if (practice is not null &&
            practice.SubscriptionStatus == Domain.Entities.SubscriptionStatus.Trial &&
            practice.TrialEndsAt.HasValue &&
            practice.TrialEndsAt.Value < DateTimeOffset.UtcNow)
        {
            await sender.Send(
                new SetPracticeSubscriptionCommand(
                    practice.Id,
                    Domain.Entities.SubscriptionTier.Starter,
                    Domain.Entities.SubscriptionStatus.Active,
                    null),
                cancellationToken);
            practice = await practiceRepository.GetByIdAsync(user.PracticeId, cancellationToken);
        }

        var roles = user.RoleAssignments
            .Where(ra => ra.Role?.IsActive == true)
            .Select(ra => ra.Role.Name)
            .ToList();

        var permissions = user.RoleAssignments
            .Where(ra => ra.Role?.IsActive == true)
            .SelectMany(ra => ra.Role.GetPermissionKeys())
            .Distinct()
            .ToList();

        var subscriptionStatus = practice?.SubscriptionStatus ?? Domain.Entities.SubscriptionStatus.Trial;
        var subscriptionTier = practice?.SubscriptionTier ?? Domain.Entities.SubscriptionTier.Starter;
        var accessToken = tokenService.GenerateJwtToken(user, permissions, subscriptionStatus, subscriptionTier);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        await writeRepository.AddRefreshTokenAsync(user.Id, refreshTokenValue, jwtSettings.Value.RefreshTokenExpiryDays, cancellationToken);

        return new LoginUserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role.ToString(),
            roles,
            permissions,
            user.PracticeId,
            practice?.Name ?? string.Empty,
            practice?.PrimaryColor,
            practice?.LogoBase64,
            practice?.HasDispensary ?? false,
            practice?.SubscriptionTier ?? Domain.Entities.SubscriptionTier.Starter,
            practice?.SubscriptionStatus ?? Domain.Entities.SubscriptionStatus.Trial,
            practice?.TrialEndsAt,
            accessToken,
            refreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.ExpiryMinutes));
    }
}
