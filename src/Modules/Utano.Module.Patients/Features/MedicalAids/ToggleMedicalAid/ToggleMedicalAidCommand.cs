using MediatR;
using Utano.Module.Core.Authorization;

namespace Utano.Module.Patients.Features.MedicalAids.ToggleMedicalAid;

public record ActivateMedicalAidCommand(Guid Id) : IRequest, IRequirePermission
{
    public string Permission => "settings.medical_aids";
}

public record DeactivateMedicalAidCommand(Guid Id) : IRequest, IRequirePermission
{
    public string Permission => "settings.medical_aids";
}
