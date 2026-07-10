using MediatR;

namespace Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

public record AddMedicalAidCommand(string Name, string Code) : IRequest<MedicalAidResponse>;

public record MedicalAidResponse(Guid Id, string Name, string Code, bool IsActive);
