using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.ResetPassword;

[ApiController]
[Route("api/auth")]
public class ResetPasswordEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("reset-password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Complete a password reset using the token from the reset email")]
    [Tags("Identity Module")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        var ok = await sender.Send(command, ct);
        return ok ? NoContent() : BadRequest(new { message = "This reset link is invalid or has expired." });
    }
}

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<bool>;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy();
    }
}

public class ResetPasswordHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPasswordService passwordService,
    IValidator<ResetPasswordCommand> validator)
    : IRequestHandler<ResetPasswordCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var tokenHash = PasswordResetTokenHasher.Hash(command.Token);
        var resetToken = await readRepository.GetValidPasswordResetTokenAsync(tokenHash, ct);
        if (resetToken is null) return false;

        var user = await readRepository.GetByIdAsync(resetToken.UserId, ct);
        if (user is null) return false;

        user.UpdatePassword(passwordService.Hash(command.NewPassword));
        await writeRepository.UpdateAsync(user, ct);
        await writeRepository.MarkPasswordResetTokenUsedAsync(resetToken.Id, ct);

        return true;
    }
}
