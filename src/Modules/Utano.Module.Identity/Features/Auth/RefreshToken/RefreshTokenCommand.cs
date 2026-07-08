using MediatR;

namespace Utano.Module.Identity.Features.Auth.RefreshToken;

public record RefreshTokenCommand(
    Guid UserId,
    string Token
) : IRequest<RefreshTokenResponse>;
