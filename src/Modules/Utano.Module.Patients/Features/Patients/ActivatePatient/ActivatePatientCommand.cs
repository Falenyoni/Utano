using MediatR;

namespace Utano.Module.Patients.Features.Patients.ActivatePatient;

public record ActivatePatientCommand(Guid Id) : IRequest<bool>;
