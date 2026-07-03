using MediatR;

namespace Utano.Module.Patients.Features.Patients.DeactivatePatient;

public record DeactivatePatientCommand(Guid Id) : IRequest<bool>;
