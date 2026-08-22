using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.DeactivateUser;

public class DeactivateUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    ILogger<DeactivateUserHandler> logger)
    : IRequestHandler<DeactivateUserCommand>
{
    public async Task Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null || user.PracticeId != currentUserService.PracticeId)
            throw new UtanoDomainException("User not found.");

        user.Deactivate();
        await writeRepository.UpdateAsync(user, cancellationToken);

        try
        {
            await auditService.LogAsync("User", user.Id.ToString(), "Deactivated",
                $"Name: {user.FullName}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log user deactivation for {UserId}", user.Id);
        }
    }
}
