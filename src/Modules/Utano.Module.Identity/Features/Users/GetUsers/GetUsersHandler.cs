using MediatR;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Enums;
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
        if (!string.IsNullOrEmpty(query.Role) && Enum.TryParse<UserRole>(query.Role, true, out var role))
        {
            var byRole = await readRepository.GetByRoleAsync(currentUserService.PracticeId, role, cancellationToken);
            return byRole.Select(Map).ToList().AsReadOnly();
        }

        var all = await readRepository.GetAllByPracticeAsync(currentUserService.PracticeId, cancellationToken);
        return all.Select(Map).ToList().AsReadOnly();
    }

    private static UserSummaryResponse Map(Domain.Entities.User u) =>
        new(u.Id, u.FullName, u.Email.Value, u.Role.ToString(), u.Status.ToString(),
            u.RoleAssignments.Select(ra => ra.RoleId).ToList());
}
