using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Configuration;
using Utano.Module.Identity.Domain.Enums;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.LoginUser;

public class LoginUserHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPracticeRepository practiceRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    IOptions<JwtSettings> jwtSettings,
    IValidator<LoginUserCommand> validator)
    : IRequestHandler<LoginUserCommand, LoginUserResponse>
{
    public async Task<LoginUserResponse> Handle(
        LoginUserCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var user = await readRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !passwordService.Verify(command.Password, user.PasswordHash))
            throw new UtanoDomainException("Invalid email or password.");

        if (user.Status != UserStatus.Active)
            throw new UtanoDomainException("Your account is not active. Contact your administrator.");

        var practice = await practiceRepository.GetByIdAsync(user.PracticeId, cancellationToken);

        var accessToken = tokenService.GenerateJwtToken(user);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        await writeRepository.AddRefreshTokenAsync(user.Id, refreshTokenValue, jwtSettings.Value.RefreshTokenExpiryDays, cancellationToken);

        return new LoginUserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role.ToString(),
            user.PracticeId,
            practice?.Name ?? string.Empty,
            accessToken,
            refreshTokenValue,
            DateTimeOffset.UtcNow.AddMinutes(jwtSettings.Value.ExpiryMinutes));
    }
}
