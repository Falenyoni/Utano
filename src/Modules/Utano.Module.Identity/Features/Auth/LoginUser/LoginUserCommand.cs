using MediatR;

namespace Utano.Module.Identity.Features.Auth.LoginUser;

public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<LoginUserResponse>;
