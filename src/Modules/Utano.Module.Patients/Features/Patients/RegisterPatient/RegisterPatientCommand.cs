using MediatR;

namespace Utano.Module.Patients.Features.Patients.RegisterPatient;

public record RegisterPatientCommand(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string Gender,
    string NationalId,
    List<RegisterPatientContactRequest> Contacts,
    List<RegisterPatientAddressRequest>? Addresses
) : IRequest<RegisterPatientResponse>;

public record RegisterPatientContactRequest(
    string Type,
    string PhoneNumber,
    string? Email,
    bool IsPrimary
);

public record RegisterPatientAddressRequest(
    string Type,
    string Street,
    string? Suburb,
    string City,
    string Country,
    bool IsPrimary
);
