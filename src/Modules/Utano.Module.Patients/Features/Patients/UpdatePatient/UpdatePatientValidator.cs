using FluentValidation;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Features.Patients.UpdatePatient;

public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Patient ID is required.");

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

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .When(x => x.MiddleName is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .When(x => x.Notes is not null);
    }
}
