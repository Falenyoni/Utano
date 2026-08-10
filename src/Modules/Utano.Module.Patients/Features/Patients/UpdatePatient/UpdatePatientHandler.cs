using FluentValidation;
using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Enums;
using Utano.Module.Patients.Domain.Interfaces;
using Utano.Module.Patients.Domain.ValueObjects;

namespace Utano.Module.Patients.Features.Patients.UpdatePatient;

public class UpdatePatientHandler(
    IPatientReadRepository readRepository,
    IPatientWriteRepository writeRepository,
    IValidator<UpdatePatientCommand> validator)
    : IRequestHandler<UpdatePatientCommand, bool>
{
    public async Task<bool> Handle(UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new UtanoDomainException(validationResult.Errors[0].ErrorMessage);

        var patient = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (patient is null)
            return false;

        var fullName = FullName.Create(command.FirstName, command.LastName, command.MiddleName ?? "");
        patient.UpdateDetails(fullName, command.Notes, command.Occupation);
        patient.UpdateMedicalAid(command.MedicalAidId, command.MedicalAidNumber);

        var identifierType = Enum.Parse<PatientIdentifierType>(command.IdentifierType, ignoreCase: true);
        if (identifierType != patient.Identifier.Type || command.IdentifierValue != patient.Identifier.Value)
        {
            if (identifierType != PatientIdentifierType.Pending)
            {
                var existing = await readRepository.GetByIdentifierAsync(
                    identifierType, command.IdentifierValue!, cancellationToken);
                if (existing is not null && existing.Id != patient.Id)
                    throw new UtanoDomainException($"A patient with this {identifierType} is already registered.");
            }

            patient.UpdateIdentifier(PatientIdentifier.Create(identifierType, command.IdentifierValue));
        }

        if (command.BloodGroup is not null || command.Allergies is not null || command.ChronicConditions is not null)
        {
            var bloodGroup = string.IsNullOrWhiteSpace(command.BloodGroup)
                ? (BloodGroup?)null
                : Enum.Parse<BloodGroup>(command.BloodGroup, ignoreCase: true);
            patient.UpdateMedicalHistory(bloodGroup, command.Allergies, command.ChronicConditions);
        }

        await writeRepository.UpdateAsync(patient, cancellationToken);
        return true;
    }
}
