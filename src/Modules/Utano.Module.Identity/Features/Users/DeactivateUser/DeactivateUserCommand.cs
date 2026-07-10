using MediatR;

namespace Utano.Module.Identity.Features.Users.DeactivateUser;

public record DeactivateUserCommand(Guid Id) : IRequest;
