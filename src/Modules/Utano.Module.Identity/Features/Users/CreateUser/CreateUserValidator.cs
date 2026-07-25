using FluentValidation;
using Utano.Module.Core.Modules;

namespace Utano.Module.Identity.Features.Users.CreateUser;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => SystemRoles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Invalid role. Must be one of: {string.Join(", ", SystemRoles.All)}.");
    }
}
