namespace Utano.Module.Patients.Features.Patients.RegisterPatient;

public record RegisterPatientResponse(
    Guid Id,
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Gender,
    string Status,
    Guid? MedicalAidId,
    string? MedicalAidNumber,
    DateTimeOffset CreatedAt
);
