using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.ActivatePatient;

public class ActivatePatientHandler(
    IPatientReadRepository readRepository,
    IPatientWriteRepository writeRepository,
    IAuditService auditService,
    ILogger<ActivatePatientHandler> logger)
    : IRequestHandler<ActivatePatientCommand, bool>
{
    public async Task<bool> Handle(ActivatePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await readRepository.GetByIdAsync(command.Id, cancellationToken);

        if (patient is null)
            return false;

        patient.Activate();
        await writeRepository.UpdateAsync(patient, cancellationToken);

        try
        {
            await auditService.LogAsync("Patient", patient.Id.ToString(), "Activated",
                $"Patient: {patient.FullName.Display}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log patient activation for {PatientId}", patient.Id);
        }

        return true;
    }
}
