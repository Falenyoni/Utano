using FluentValidation;
using MediatR;
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
    IValidator<RegisterPatientCommand> validator)
    : IRequestHandler<RegisterPatientCommand, RegisterPatientResponse>
{
    public async Task<RegisterPatientResponse> Handle(
        RegisterPatientCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new UtanoDomainException(validationResult.Errors[0].ErrorMessage);

        var existing = await readRepository.GetByNationalIdAsync(
            command.NationalId, cancellationToken);

        if (existing is not null)
            throw new UtanoDomainException($"A patient with National ID '{command.NationalId}' is already registered.");

        var fullName = FullName.Create(command.FirstName, command.LastName, command.MiddleName);
        var nationalId = NationalId.Create(command.NationalId);
        var gender = Enum.Parse<Gender>(command.Gender, ignoreCase: true);

        var patient = Patient.Register(
            currentUserService.PracticeId,
            fullName,
            command.DateOfBirth,
            gender,
            nationalId);

        foreach (var c in command.Contacts)
            patient.AddContact(c.Type, c.PhoneNumber, c.Email, c.IsPrimary);

        if (command.Addresses is not null)
            foreach (var a in command.Addresses)
                patient.AddAddress(a.Type, a.Street, a.City, a.Country, a.Suburb, a.IsPrimary);

        await writeRepository.AddAsync(patient, cancellationToken);

        return new RegisterPatientResponse(
            patient.Id,
            patient.FullName.Display,
            patient.NationalId.Value,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.Status.ToString(),
            patient.CreatedAt);
    }
}
