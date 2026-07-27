using FluentValidation;

namespace Utano.Module.Identity.Features;

public static class PasswordPolicy
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");
    }
}
