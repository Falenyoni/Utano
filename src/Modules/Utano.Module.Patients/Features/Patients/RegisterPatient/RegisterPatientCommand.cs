using MediatR;
using Utano.Module.Patients.Domain.Enums;

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
    ContactType Type,
    string PhoneNumber,
    string? Email,
    bool IsPrimary
);

public record RegisterPatientAddressRequest(
    AddressType Type,
    string Street,
    string? Suburb,
    string City,
    string Country,
    bool IsPrimary
);
