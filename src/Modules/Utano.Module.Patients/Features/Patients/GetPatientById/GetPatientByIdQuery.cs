using MediatR;

namespace Utano.Module.Patients.Features.Patients.GetPatientById;

public record GetPatientByIdQuery(Guid Id) : IRequest<GetPatientByIdResponse?>;
