namespace Utano.Module.Patients.Features.Patients.GetPatients;

public record GetPatientsResponse(
    Guid Id,
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Gender,
    string Status
);
