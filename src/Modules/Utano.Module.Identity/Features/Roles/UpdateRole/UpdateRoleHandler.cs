using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Roles.UpdateRole;

public class UpdateRoleHandler(IdentityDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(
                r => r.Id == command.Id && r.PracticeId == currentUser.PracticeId,
                cancellationToken);

        if (role is null)
            throw new UtanoDomainException("Role not found.");

        var nameTaken = await db.Roles.AnyAsync(
            r => r.PracticeId == currentUser.PracticeId && r.Name == command.Name && r.Id != command.Id,
            cancellationToken);

        if (nameTaken)
            throw new UtanoDomainException($"A role named '{command.Name}' already exists.");

        role.Update(command.Name, command.Description);
        role.SetPermissions(command.Permissions);

        if (command.IsActive && !role.IsActive) role.Activate();
        else if (!command.IsActive && role.IsActive) role.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }
}
