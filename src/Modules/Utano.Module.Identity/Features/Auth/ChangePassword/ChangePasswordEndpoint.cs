using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.ChangePassword;

[ApiController]
[Route("api/auth")]
[Authorize]
public class ChangePasswordEndpoint(ISender sender) : ControllerBase
{
    [HttpPut("password")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Change the currently authenticated user's password")]
    [Tags("Identity Module")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordBody body, CancellationToken ct)
    {
        await sender.Send(new ChangePasswordCommand(body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }
}

public record ChangePasswordBody(string CurrentPassword, string NewPassword);
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy();
    }
}

public class ChangePasswordHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPasswordService passwordService,
    ICurrentUserService currentUser,
    IValidator<ChangePasswordCommand> validator)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var user = await readRepository.GetByIdAsync(currentUser.UserId, ct)
            ?? throw new UtanoDomainException("User not found.");

        if (!passwordService.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UtanoDomainException("Current password is incorrect.");

        user.UpdatePassword(passwordService.Hash(cmd.NewPassword));
        await writeRepository.UpdateAsync(user, ct);
    }
}