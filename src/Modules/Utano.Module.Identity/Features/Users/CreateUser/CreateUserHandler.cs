using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.CreateUser;

public class CreateUserHandler(
    IUserWriteRepository writeRepository,
    IPasswordService passwordService,
    ICurrentUserService currentUserService,
    IdentityDbContext db,
    IAuditService auditService,
    IValidator<CreateUserCommand> validator,
    ILogger<CreateUserHandler> logger)
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var passwordHash = passwordService.Hash(command.Password);

        var user = User.Create(
            currentUserService.PracticeId,
            command.FirstName,
            command.LastName,
            command.Email,
            passwordHash,
            command.Role);

        if (!string.IsNullOrWhiteSpace(command.Specialty))
            user.SetSpecialty(command.Specialty);

        await writeRepository.AddAsync(user, cancellationToken);

        var systemRole = await db.Roles.FirstOrDefaultAsync(
            r => r.PracticeId == currentUserService.PracticeId
              && r.Name == command.Role
              && r.IsSystem,
            cancellationToken);

        if (systemRole is not null)
        {
            db.UserRoles.Add(new UserRoleAssignment(user.Id, systemRole.Id));
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await auditService.LogAsync("User", user.Id.ToString(), "Created",
                $"Name: {user.FullName} · Role: {user.Role}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log user creation for {UserId}", user.Id);
        }

        return new CreateUserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role,
            user.Status.ToString(),
            user.CreatedAt);
    }
}
