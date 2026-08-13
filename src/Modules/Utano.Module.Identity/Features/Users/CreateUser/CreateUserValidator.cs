using FluentValidation;
using Utano.Module.Core.Modules;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Users.CreateUser;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserReadRepository userReadRepository)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);

        // Global, not per-practice - GetByEmailAsync (used at login) has no PracticeId filter and no
        // deterministic ordering, so a second practice with this same email would make login
        // undefined/unreachable for whichever account doesn't happen to come back first. Matches the
        // same check CreatePracticeValidator already does for the admin email at signup.
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) => !await userReadRepository.EmailExistsAsync(email, ct))
            .WithMessage("An account with this email already exists.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => SystemRoles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Invalid role. Must be one of: {string.Join(", ", SystemRoles.All)}.");
    }
}
