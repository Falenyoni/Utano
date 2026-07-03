using MediatR;

namespace Utano.Module.Patients.Features.Patients.UpdatePatient;

public record UpdatePatientCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    string? Notes
) : IRequest<bool>;
