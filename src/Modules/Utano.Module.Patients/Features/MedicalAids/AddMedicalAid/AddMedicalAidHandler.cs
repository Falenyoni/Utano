using MediatR;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Domain.Entities;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

public class AddMedicalAidHandler(
    IMedicalAidRepository repository,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddMedicalAidCommand, MedicalAidResponse>
{
    public async Task<MedicalAidResponse> Handle(AddMedicalAidCommand command, CancellationToken cancellationToken)
    {
        var aid = MedicalAid.Create(currentUserService.PracticeId, command.Name, command.Code);
        await repository.AddAsync(aid, cancellationToken);
        return new MedicalAidResponse(aid.Id, aid.Name, aid.Code, aid.IsActive);
    }
}
