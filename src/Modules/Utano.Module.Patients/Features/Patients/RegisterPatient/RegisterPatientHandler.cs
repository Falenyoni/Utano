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
            command.NationalId, currentUserService.PracticeId, cancellationToken);

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

        foreach (var contact in command.Contacts)
            patient.AddContact(contact.Type, contact.PhoneNumber, contact.Email, contact.IsPrimary);

        if (command.Addresses is not null)
            foreach (var address in command.Addresses)
                patient.AddAddress(address.Type, address.Street, address.City,
                    address.Country, address.Suburb, address.IsPrimary);

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
