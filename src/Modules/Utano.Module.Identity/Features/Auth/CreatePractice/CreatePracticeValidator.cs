using FluentValidation;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Auth.CreatePractice;

public class CreatePracticeValidator : AbstractValidator<CreatePracticeCommand>
{
    public CreatePracticeValidator(IUserReadRepository userReadRepository)
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ContactPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PhysicalAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();

        // Was missing entirely before - meant two practices could end up with the same admin
        // email, which login can't disambiguate. Matters more now that CreatePracticeCommand is
        // also reachable through the public self-signup endpoint, not just the API-key-gated one.
        RuleFor(x => x.AdminEmail)
            .MustAsync(async (email, ct) => !await userReadRepository.EmailExistsAsync(email, ct))
            .WithMessage("An account with this email already exists.")
            .When(x => !string.IsNullOrWhiteSpace(x.AdminEmail));

        RuleFor(x => x.AdminPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");
    }
}
