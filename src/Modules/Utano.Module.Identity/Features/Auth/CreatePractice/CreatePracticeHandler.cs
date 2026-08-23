using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Configuration;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Interfaces;
using Utano.Module.Identity.Features.Auth;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public class CreatePracticeHandler(
    IPracticeRepository practiceRepository,
    IUserWriteRepository userWriteRepository,
    IPasswordService passwordService,
    IdentityDbContext db,
    IEnumerable<IModuleDescriptor> moduleDescriptors,
    IValidator<CreatePracticeCommand> validator,
    IEmailSender emailSender,
    IOptions<EmailVerificationSettings> emailVerificationSettings,
    ILogger<CreatePracticeHandler> logger)
    : IRequestHandler<CreatePracticeCommand, CreatePracticeResponse>
{
    // Keyed off SystemRoles.All so adding a new system role only requires adding its description
    // here, instead of also remembering a separate SeedRole(...) call in Handle() below. Missing
    // an entry throws (via RoleDescriptions[name]) rather than silently seeding no description.
    private static readonly IReadOnlyDictionary<string, string> RoleDescriptions = new Dictionary<string, string>
    {
        [SystemRoles.Admin] = "Full system access",
        [SystemRoles.Doctor] = "Patient care and clinical documentation",
        [SystemRoles.Nurse] = "Patient care and appointment management",
        [SystemRoles.Receptionist] = "Patient registration and scheduling",
        [SystemRoles.Billing] = "Financial management and reporting",
        [SystemRoles.Triage] = "Patient triage and initial assessment",
    };

    public async Task<CreatePracticeResponse> Handle(
        CreatePracticeCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var practice = Practice.Create(
            command.Name, command.ContactEmail,
            command.ContactPhone, command.PhysicalAddress);

        // Trial doesn't start yet - StartTrial(30) happens when the admin verifies their email
        // (see VerifyEmailHandler), so a slow-to-verify signup doesn't lose trial days, and an
        // unverified signup never gets a live trial at all.
        await practiceRepository.AddAsync(practice, cancellationToken);

        var passwordHash = passwordService.Hash(command.AdminPassword);

        var admin = User.Create(
            practice.Id,
            command.AdminFirstName,
            command.AdminLastName,
            command.AdminEmail,
            passwordHash,
            SystemRoles.Admin,
            emailVerified: false);

        await userWriteRepository.AddAsync(admin, cancellationToken);

        var roles = SystemRoles.All.Select(name => SeedRole(practice.Id, name, RoleDescriptions[name])).ToList();
        db.Roles.AddRange(roles);

        var adminRole = roles.Single(r => r.Name == SystemRoles.Admin);
        db.UserRoles.Add(new UserRoleAssignment(admin.Id, adminRole.Id));

        // Trial starts with full Professional access so practices see all features
        var features = SeedFeaturesForTier(practice.Id, Domain.Entities.SubscriptionTier.Professional);
        db.PracticeFeatures.AddRange(features);

        await db.SaveChangesAsync(cancellationToken);

        var rawToken = EmailVerificationTokenHasher.GenerateToken();
        var tokenHash = EmailVerificationTokenHasher.Hash(rawToken);
        await userWriteRepository.AddEmailVerificationTokenAsync(
            admin.Id, tokenHash, emailVerificationSettings.Value.ExpiryMinutes, cancellationToken);

        var verifyUrl = $"{emailVerificationSettings.Value.FrontendBaseUrl}/verify-email?token={rawToken}";
        var html = $"""
            <p>Hi {admin.FirstName},</p>
            <p>Welcome to Utano. Click the link below to verify your email and activate your 30-day trial.</p>
            <p><a href="{verifyUrl}">Verify your email</a></p>
            <p>This link expires in {emailVerificationSettings.Value.ExpiryMinutes / 60} hours.</p>
            """;

        try
        {
            await emailSender.SendAsync(admin.Email.Value, "Verify your email to activate your Utano trial", html, cancellationToken);
        }
        catch (Exception ex)
        {
            // Swallowed deliberately - the practice/admin record already exists at this point, and
            // failing the whole signup because the confirmation email didn't send would be worse
            // than the admin having to use "resend verification" once they notice nothing arrived.
            logger.LogError(ex, "Failed to send verification email to user {UserId}", admin.Id);
        }

        return new CreatePracticeResponse(practice.Id, practice.Name, admin.Id, admin.Email.Value);
    }

    private Role SeedRole(Guid practiceId, string name, string description)
    {
        var permissions = moduleDescriptors
            .SelectMany(m => m.GetPermissionsForRole(name))
            .Distinct()
            .ToList();

        var role = Role.Create(practiceId, name, description, isSystem: true);
        role.SetPermissions(permissions);
        return role;
    }

    internal IEnumerable<PracticeFeature> SeedFeaturesForTier(Guid practiceId, string tier)
    {
        var allowedPlans = tier == Domain.Entities.SubscriptionTier.Professional
            ? new[] { "free", "professional" }
            : new[] { "free" };

        return moduleDescriptors
            .Where(m => allowedPlans.Contains(m.Plan))
            .Select(m => m.FeatureKey)
            .Distinct()
            .Select(key => PracticeFeature.Create(practiceId, key));
    }
}
