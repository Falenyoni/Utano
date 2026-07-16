using FluentValidation;
using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Constants;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public class CreatePracticeHandler(
    IPracticeRepository practiceRepository,
    IUserWriteRepository userWriteRepository,
    IPasswordService passwordService,
    IdentityDbContext db,
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
            UserRole.Admin);

        await userWriteRepository.AddAsync(admin, cancellationToken);

        var adminRole = SeedRole(practice.Id, "Admin", "Full system access", Permissions.AdminPermissions);
        var doctorRole = SeedRole(practice.Id, "Doctor", "Patient care and clinical documentation", Permissions.DoctorPermissions);
        var nurseRole = SeedRole(practice.Id, "Nurse", "Patient care and appointment management", Permissions.NursePermissions);
        var receptionistRole = SeedRole(practice.Id, "Receptionist", "Patient registration and scheduling", Permissions.ReceptionistPermissions);
        var billingRole = SeedRole(practice.Id, "Billing", "Financial management and reporting", Permissions.BillingPermissions);

        db.Roles.AddRange([adminRole, doctorRole, nurseRole, receptionistRole, billingRole]);
        db.UserRoles.Add(new UserRoleAssignment(admin.Id, adminRole.Id));
        await db.SaveChangesAsync(cancellationToken);

        return new CreatePracticeResponse(practice.Id, practice.Name, admin.Id, admin.Email.Value);
    }

    private static Role SeedRole(Guid practiceId, string name, string description, IReadOnlyList<string> permissions)
    {
        var role = Role.Create(practiceId, name, description, isSystem: true);
        role.SetPermissions(permissions);
        return role;
    }
}
