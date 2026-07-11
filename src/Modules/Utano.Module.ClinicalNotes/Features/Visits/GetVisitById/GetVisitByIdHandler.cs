using MediatR;
using Utano.Module.ClinicalNotes.Domain.Interfaces;

namespace Utano.Module.ClinicalNotes.Features.Visits.GetVisitById;

public class GetVisitByIdHandler(IVisitReadRepository readRepository)
    : IRequestHandler<GetVisitByIdQuery, VisitDetailResponse?>
{
    public async Task<VisitDetailResponse?> Handle(GetVisitByIdQuery query, CancellationToken cancellationToken)
    {
        var v = await readRepository.GetByIdAsync(query.Id, cancellationToken);
        if (v is null) return null;

        return new VisitDetailResponse(
            v.Id, v.PatientId, v.PatientName, v.DoctorId, v.DoctorName, v.VisitDate,
            v.BloodPressureSystolic, v.BloodPressureDiastolic,
            v.WeightKg, v.HeightCm, v.TemperatureCelsius, v.PulseRate, v.OxygenSaturation,
            v.Department,
            v.ChiefComplaint, v.Symptoms, v.Diagnosis, v.Treatment, v.Prescription, v.Notes,
            v.Status.ToString(), v.CreatedAt, v.UpdatedAt);
    }
}
