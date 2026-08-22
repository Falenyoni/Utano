using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.DeactivatePatient;

public class DeactivatePatientHandler(
    IPatientReadRepository readRepository,
    IPatientWriteRepository writeRepository,
    IAuditService auditService,
    ILogger<DeactivatePatientHandler> logger)
    : IRequestHandler<DeactivatePatientCommand, bool>
{
    public async Task<bool> Handle(DeactivatePatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await readRepository.GetByIdAsync(command.Id, cancellationToken);

        if (patient is null)
            return false;

        patient.Deactivate();
        await writeRepository.UpdateAsync(patient, cancellationToken);

        try
        {
            await auditService.LogAsync("Patient", patient.Id.ToString(), "Deactivated",
                $"Patient: {patient.FullName.Display}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log patient deactivation for {PatientId}", patient.Id);
        }

        return true;
    }
}
