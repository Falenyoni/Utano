using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;

namespace Utano.Module.Identity.Features.Roles.CreateRole;

public class CreateRoleHandler(
    IdentityDbContext db,
    ICurrentUserService currentUser,
    IValidator<CreateRoleCommand> validator)
    : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var exists = await db.Roles.AnyAsync(
            r => r.PracticeId == currentUser.PracticeId && r.Name == command.Name,
            cancellationToken);

        if (exists)
            throw new UtanoDomainException($"A role named '{command.Name}' already exists.");

        var role = Role.Create(currentUser.PracticeId, command.Name, command.Description);
        role.SetPermissions(command.Permissions);

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}
