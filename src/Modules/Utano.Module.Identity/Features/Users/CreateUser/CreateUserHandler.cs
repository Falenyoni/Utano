using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    IValidator<CreateUserCommand> validator)
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

        return new CreateUserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role,
            user.Status.ToString(),
            user.CreatedAt);
    }
}
