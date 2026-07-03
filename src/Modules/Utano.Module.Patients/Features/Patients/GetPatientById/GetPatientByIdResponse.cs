namespace Utano.Module.Patients.Features.Patients.GetPatientById;

public record GetPatientByIdResponse(
    Guid Id,
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Gender,
    string Status,
    string? Notes,
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
