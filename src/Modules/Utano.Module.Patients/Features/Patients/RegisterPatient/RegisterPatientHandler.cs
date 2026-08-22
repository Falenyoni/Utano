using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Enums;
using Utano.Module.Patients.Domain.Interfaces;
using Utano.Module.Patients.Domain.ValueObjects;

namespace Utano.Module.Patients.Features.Patients.RegisterPatient;

public class RegisterPatientHandler(
    IPatientWriteRepository writeRepository,
    IPatientReadRepository readRepository,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    IValidator<RegisterPatientCommand> validator,
    ILogger<RegisterPatientHandler> logger)
    : IRequestHandler<RegisterPatientCommand, RegisterPatientResponse>
{
    public async Task<RegisterPatientResponse> Handle(
        RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new UtanoDomainException(validationResult.Errors[0].ErrorMessage);

        var identifierType = Enum.Parse<PatientIdentifierType>(command.IdentifierType, ignoreCase: true);

        if (identifierType != PatientIdentifierType.Pending)
        {
            var existing = await readRepository.GetByIdentifierAsync(
                identifierType, command.IdentifierValue!, cancellationToken);

            if (existing is not null)
                throw new UtanoDomainException($"A patient with this {identifierType} is already registered.");
        }

        var fullName = FullName.Create(command.FirstName, command.LastName, command.MiddleName ?? "");
        var identifier = PatientIdentifier.Create(identifierType, command.IdentifierValue);
        var gender = Enum.Parse<Gender>(command.Gender, ignoreCase: true);

        var patient = Patient.Register(
            currentUserService.PracticeId,
            fullName,
            command.DateOfBirth,
            gender,
            identifier);

        if (!string.IsNullOrWhiteSpace(command.Occupation))
            patient.SetOccupation(command.Occupation);

        foreach (var c in command.Contacts)
            patient.AddContact(c.Type, c.PhoneNumber, c.Email, c.IsPrimary);

        if (command.Addresses is not null)
            foreach (var a in command.Addresses)
                patient.AddAddress(a.Type, a.Street, a.City, a.Country, a.Suburb, a.IsPrimary);

        if (command.MedicalAidId.HasValue || !string.IsNullOrWhiteSpace(command.MedicalAidNumber))
            patient.UpdateMedicalAid(command.MedicalAidId, command.MedicalAidNumber);

        if (!string.IsNullOrWhiteSpace(command.BloodGroup) || !string.IsNullOrWhiteSpace(command.Allergies) || !string.IsNullOrWhiteSpace(command.ChronicConditions))
        {
            var bloodGroup = string.IsNullOrWhiteSpace(command.BloodGroup) ? (Domain.Enums.BloodGroup?)null
                : Enum.Parse<Domain.Enums.BloodGroup>(command.BloodGroup, ignoreCase: true);
            patient.UpdateMedicalHistory(bloodGroup, command.Allergies, command.ChronicConditions);
        }

        await writeRepository.AddAsync(patient, cancellationToken);

        try
        {
            await auditService.LogAsync("Patient", patient.Id.ToString(), "Registered",
                $"Patient: {patient.FullName.Display}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log patient registration for {PatientId}", patient.Id);
        }

        return new RegisterPatientResponse(
            patient.Id,
            patient.FullName.Display,
            patient.Identifier.Type.ToString(),
            patient.Identifier.Value,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.Status.ToString(),
            patient.MedicalAidId,
            patient.MedicalAidNumber,
            patient.CreatedAt);
    }
}
