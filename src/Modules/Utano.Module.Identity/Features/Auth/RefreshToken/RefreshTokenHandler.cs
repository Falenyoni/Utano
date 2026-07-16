using MediatR;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Configuration;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.RefreshToken;

public class RefreshTokenHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var user = await readRepository.GetByIdWithTokensAsync(command.UserId, cancellationToken);

        if (user is null)
            throw new UtanoDomainException("Invalid token.");

        var existing = user.RefreshTokens.FirstOrDefault(t => t.Token == command.Token);

        if (existing is null || !existing.IsActive)
            throw new UtanoDomainException("Refresh token is invalid or has expired.");

        var permissions = user.RoleAssignments
            .Where(ra => ra.Role?.IsActive == true)
            .SelectMany(ra => ra.Role.GetPermissionKeys())
            .Distinct()
            .ToList();

        var newAccessToken = tokenService.GenerateJwtToken(user, permissions);
        var newRefreshTokenValue = tokenService.GenerateRefreshToken();

        // Revoke all active tokens — they are tracked entities, EF detects the change
        foreach (var t in user.RefreshTokens.Where(t => t.IsActive))
            t.Revoke();

        // Insert the new token directly; SaveChangesAsync also persists the revocations above
        await writeRepository.AddRefreshTokenAsync(user.Id, newRefreshTokenValue, jwtSettings.Value.RefreshTokenExpiryDays, cancellationToken);

        return new RefreshTokenResponse(
            newAccessToken,
            newRefreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.ExpiryMinutes));
    }
}
