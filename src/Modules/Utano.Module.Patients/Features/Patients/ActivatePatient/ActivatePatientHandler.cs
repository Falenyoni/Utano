using MediatR;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.ActivatePatient;

public class ActivatePatientHandler(
    IPatientReadRepository readRepository,
    IPatientWriteRepository writeRepository)
    : IRequestHandler<ActivatePatientCommand, bool>
{
    public async Task<bool> Handle(ActivatePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await readRepository.GetByIdAsync(command.Id, cancellationToken);

        if (patient is null)
            return false;

        patient.Activate();
        await writeRepository.UpdateAsync(patient, cancellationToken);
        return true;
    }
}
