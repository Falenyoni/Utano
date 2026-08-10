using FluentValidation;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.RegisterPatient;

public class RegisterPatientValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .When(x => x.MiddleName is not null);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => Enum.TryParse<Gender>(g, ignoreCase: true, out _))
            .WithMessage($"Gender must be one of: {string.Join(", ", Enum.GetNames<Gender>())}.");

        RuleFor(x => x.IdentifierType)
            .NotEmpty().WithMessage("Identifier type is required.")
            .Must(t => Enum.TryParse<PatientIdentifierType>(t, ignoreCase: true, out _))
            .WithMessage($"Identifier type must be one of: {string.Join(", ", Enum.GetNames<PatientIdentifierType>())}.");

        RuleFor(x => x.IdentifierValue)
            .NotEmpty().WithMessage("A value is required for this identifier type.")
            .MaximumLength(50)
            .When(x => !string.Equals(x.IdentifierType, nameof(PatientIdentifierType.Pending), StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.IdentifierValue)
            .Empty().WithMessage("A Pending identifier cannot have a value.")
            .When(x => string.Equals(x.IdentifierType, nameof(PatientIdentifierType.Pending), StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Contacts)
            .NotEmpty().WithMessage("At least one contact is required.");

        RuleForEach(x => x.Contacts).ChildRules(contact =>
        {
            contact.RuleFor(c => c.PhoneNumber)
                .NotEmpty().WithMessage("Contact phone number is required.")
                .MaximumLength(30);

            contact.RuleFor(c => c.Email)
                .EmailAddress().WithMessage("Contact email is not valid.")
                .When(c => c.Email is not null);
        });

        RuleForEach(x => x.Addresses).ChildRules(address =>
        {
            address.RuleFor(a => a.Street)
                .NotEmpty().WithMessage("Street is required.")
                .MaximumLength(200);

            address.RuleFor(a => a.City)
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(100);

            address.RuleFor(a => a.Country)
                .NotEmpty().WithMessage("Country is required.")
                .MaximumLength(100);
        });

        RuleFor(x => x.Contacts)
            .Must(c => c.Count(x => x.IsPrimary) == 1)
            .WithMessage("Exactly one contact must be marked as primary.")
            .When(x => x.Contacts.Count > 0);

        RuleFor(x => x.Addresses)
            .Must(a => a!.Count(x => x.IsPrimary) == 1)
            .WithMessage("Exactly one address must be marked as primary.")
            .When(x => x.Addresses is { Count: > 1 });
    }
}
