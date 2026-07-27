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
    public async Task<CreatePracticeResponse> Handle(
        CreatePracticeCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var practice = Practice.Create(
            command.Name, command.ContactEmail,
            command.ContactPhone, command.PhysicalAddress);

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

        var adminRole        = SeedRole(practice.Id, SystemRoles.Admin,        "Full system access");
        var doctorRole       = SeedRole(practice.Id, SystemRoles.Doctor,       "Patient care and clinical documentation");
        var nurseRole        = SeedRole(practice.Id, SystemRoles.Nurse,        "Patient care and appointment management");
        var receptionistRole = SeedRole(practice.Id, SystemRoles.Receptionist, "Patient registration and scheduling");
        var billingRole      = SeedRole(practice.Id, SystemRoles.Billing,      "Financial management and reporting");
        var triageRole       = SeedRole(practice.Id, SystemRoles.Triage,       "Patient triage and initial assessment");

        db.Roles.AddRange([adminRole, doctorRole, nurseRole, receptionistRole, billingRole, triageRole]);
        db.UserRoles.Add(new UserRoleAssignment(admin.Id, adminRole.Id));

        var features = moduleDescriptors
            .Select(m => m.FeatureKey)
            .Distinct()
            .Select(key => PracticeFeature.Create(practice.Id, key));
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
}
