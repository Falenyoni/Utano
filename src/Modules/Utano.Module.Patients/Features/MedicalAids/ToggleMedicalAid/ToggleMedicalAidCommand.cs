using MediatR;

namespace Utano.Module.Patients.Features.MedicalAids.ToggleMedicalAid;

public record ActivateMedicalAidCommand(Guid Id) : IRequest;
public record DeactivateMedicalAidCommand(Guid Id) : IRequest;
