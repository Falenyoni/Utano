using MediatR;
using Utano.Module.Patients.Features.MedicalAids.AddMedicalAid;

namespace Utano.Module.Patients.Features.MedicalAids.GetMedicalAids;

public record GetMedicalAidsQuery : IRequest<IReadOnlyList<MedicalAidResponse>>;
