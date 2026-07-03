using MediatR;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.DeactivatePatient;

public class DeactivatePatientHandler(
    IPatientReadRepository readRepository,
    IPatientWriteRepository writeRepository)
    : IRequestHandler<DeactivatePatientCommand, bool>
{
    public async Task<bool> Handle(DeactivatePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await readRepository.GetByIdAsync(command.Id, cancellationToken);

        if (patient is null)
            return false;

        patient.Deactivate();
        await writeRepository.UpdateAsync(patient, cancellationToken);
        return true;
    }
}
