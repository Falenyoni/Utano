using MediatR;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.GetUsers;

public class GetUsersHandler(
    IUserReadRepository readRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserSummaryResponse>>
{
    public async Task<IReadOnlyList<UserSummaryResponse>> Handle(
        GetUsersQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(query.Role) &&
            SystemRoles.All.Contains(query.Role, StringComparer.OrdinalIgnoreCase))
        {
            var byRole = await readRepository.GetByRoleAsync(
                currentUserService.PracticeId, query.Role, cancellationToken);
            return byRole.Select(Map).ToList().AsReadOnly();
        }

        var all = await readRepository.GetAllByPracticeAsync(currentUserService.PracticeId, cancellationToken);
        return all.Select(Map).ToList().AsReadOnly();
    }

    private static UserSummaryResponse Map(Domain.Entities.User u) =>
        new(u.Id, u.FullName, u.Email.Value, u.Role, u.Specialty, u.Status.ToString(),
            u.RoleAssignments.Select(ra => ra.RoleId).ToList());
}
