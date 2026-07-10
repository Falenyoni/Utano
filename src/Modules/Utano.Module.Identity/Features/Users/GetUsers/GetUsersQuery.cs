using MediatR;

namespace Utano.Module.Identity.Features.Users.GetUsers;

public record GetUsersQuery(string? Role = null) : IRequest<IReadOnlyList<UserSummaryResponse>>;
