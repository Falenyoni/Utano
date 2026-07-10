using MediatR;

namespace Utano.Module.Identity.Features.Users.CreateUser;

public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role
) : IRequest<CreateUserResponse>;
