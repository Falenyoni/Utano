namespace Utano.Module.Patients.Features.Patients.GetPatientById;

public record GetPatientByIdResponse(
    Guid Id,
    string FullName,
    string FirstName,
    string LastName,
    string? MiddleName,
    string NationalId,
    DateOnly DateOfBirth,
    string Gender,
    string Status,
    string? Notes,
    string? BloodGroup,
    string? Allergies,
    string? ChronicConditions,
    Guid? MedicalAidId,
    string? MedicalAidName,
    string? MedicalAidNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IEnumerable<PatientContactResponse> Contacts,
    IEnumerable<PatientAddressResponse> Addresses
);

public record PatientContactResponse(
    Guid Id,
    string Type,
    string PhoneNumber,
    string? Email,
    bool IsPrimary
);

public record PatientAddressResponse(
    Guid Id,
    string Type,
    string Street,
    string? Suburb,
    string City,
    string Country,
    bool IsPrimary
);
