using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.VerifyEmail;

[ApiController]
[Route("api/auth")]
public class VerifyEmailEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("verify-email")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Verify a practice admin's email and activate their trial")]
    [Tags("Identity Module")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCommand command, CancellationToken ct)
    {
        var ok = await sender.Send(command, ct);
        return ok ? NoContent() : BadRequest(new { message = "This verification link is invalid or has expired." });
    }
}

public record VerifyEmailCommand(string Token) : IRequest<bool>;

public class VerifyEmailValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}

public class VerifyEmailHandler(
    IUserReadRepository readRepository,
    IUserWriteRepository writeRepository,
    IPracticeRepository practiceRepository,
    IValidator<VerifyEmailCommand> validator)
    : IRequestHandler<VerifyEmailCommand, bool>
{
    public async Task<bool> Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            throw new UtanoDomainException(validation.Errors[0].ErrorMessage);

        var tokenHash = EmailVerificationTokenHasher.Hash(command.Token);
        var verificationToken = await readRepository.GetValidEmailVerificationTokenAsync(tokenHash, ct);
        if (verificationToken is null) return false;

        var user = await readRepository.GetByIdAsync(verificationToken.UserId, ct);
        if (user is null) return false;

        if (!user.IsEmailVerified)
        {
            user.MarkEmailVerified();
            await writeRepository.UpdateAsync(user, ct);

            var practice = await practiceRepository.GetByIdAsync(user.PracticeId, ct);
            if (practice is not null)
            {
                practice.StartTrial(30);
                await practiceRepository.UpdateAsync(practice, ct);
            }
        }

        await writeRepository.MarkEmailVerificationTokenUsedAsync(verificationToken.Id, ct);

        return true;
    }
}
