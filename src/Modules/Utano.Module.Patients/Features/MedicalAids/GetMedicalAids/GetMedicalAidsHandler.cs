using MediatR;
using Utano.Module.Patients.Domain.Interfaces;
using Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

namespace Utano.Module.Patients.Features.MedicalAids.GetMedicalAids;

public class GetMedicalAidsHandler(IMedicalAidRepository repository)
    : IRequestHandler<GetMedicalAidsQuery, IReadOnlyList<MedicalAidResponse>>
{
    public async Task<IReadOnlyList<MedicalAidResponse>> Handle(
        GetMedicalAidsQuery query, CancellationToken cancellationToken)
    {
        var aids = await repository.GetAllAsync(cancellationToken);
        return aids.Select(m => new MedicalAidResponse(m.Id, m.Name, m.Code, m.IsActive))
            .ToList().AsReadOnly();
    }
}
