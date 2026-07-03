using FluentValidation;
using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Domain.Interfaces;
using Utano.Module.Patients.Domain.ValueObjects;

namespace Utano.Module.Patients.Features.Patients.UpdatePatient;

public class UpdatePatientHandler(
    IPatientReadRepository readRepository,
    IPatientWriteRepository writeRepository,
    ICurrentUserService currentUserService,
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

        var fullName = FullName.Create(command.FirstName, command.LastName, command.MiddleName);
        patient.UpdateDetails(fullName, command.Notes);

        await writeRepository.UpdateAsync(patient, cancellationToken);
        return true;
    }
}
