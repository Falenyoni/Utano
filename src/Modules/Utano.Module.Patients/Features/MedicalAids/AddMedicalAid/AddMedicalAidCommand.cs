using MediatR;
using Utano.Module.Core.Authorization;

namespace Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

public record AddMedicalAidCommand(string Name, string Code) : IRequest<MedicalAidResponse>, IRequirePermission
{
    public string Permission => "settings.medical_aids";
}

public record MedicalAidResponse(Guid Id, string Name, string Code, bool IsActive);
