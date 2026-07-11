using MediatR;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.ClinicalNotes.Features.Visits.UpdateVisit;

public class UpdateVisitHandler(IVisitReadRepository readRepository, IVisitWriteRepository writeRepository)
    : IRequestHandler<UpdateVisitCommand, bool>
{
    public async Task<bool> Handle(UpdateVisitCommand command, CancellationToken cancellationToken)
    {
        var visit = await readRepository.GetByIdAsync(command.Id, cancellationToken);
        if (visit is null) return false;

        if (visit.Status == Domain.Enums.VisitStatus.Completed)
            throw new UtanoDomainException("Cannot update a completed visit.");

        visit.UpdateVitals(
            command.BloodPressureSystolic, command.BloodPressureDiastolic,
            command.WeightKg, command.HeightCm,
            command.TemperatureCelsius, command.PulseRate, command.OxygenSaturation);

        visit.UpdateClinicalNotes(
            command.ChiefComplaint, command.Symptoms,
            command.Diagnosis, command.Treatment,
            command.Prescription, command.Notes,
            command.Department);

        await writeRepository.UpdateAsync(visit, cancellationToken);
        return true;
    }
}
