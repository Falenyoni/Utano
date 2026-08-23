using FluentValidation;
using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Modules;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public class CreatePracticeHandler(
    IPracticeRepository practiceRepository,
    IUserWriteRepository userWriteRepository,
    IPasswordService passwordService,
    IdentityDbContext db,
    IEnumerable<IModuleDescriptor> moduleDescriptors,
    IValidator<CreatePracticeCommand> validator)
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

        practice.StartTrial(30);

        await practiceRepository.AddAsync(practice, cancellationToken);

        var passwordHash = passwordService.Hash(command.AdminPassword);

        var admin = User.Create(
            practice.Id,
            command.AdminFirstName,
            command.AdminLastName,
            command.AdminEmail,
            passwordHash,
            SystemRoles.Admin);

        await userWriteRepository.AddAsync(admin, cancellationToken);

        var roles = SystemRoles.All.Select(name => SeedRole(practice.Id, name, RoleDescriptions[name])).ToList();
        db.Roles.AddRange(roles);

        var adminRole = roles.Single(r => r.Name == SystemRoles.Admin);
        db.UserRoles.Add(new UserRoleAssignment(admin.Id, adminRole.Id));

        // Trial starts with full Professional access so practices see all features
        var features = SeedFeaturesForTier(practice.Id, Domain.Entities.SubscriptionTier.Professional);
        db.PracticeFeatures.AddRange(features);

        await db.SaveChangesAsync(cancellationToken);

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
