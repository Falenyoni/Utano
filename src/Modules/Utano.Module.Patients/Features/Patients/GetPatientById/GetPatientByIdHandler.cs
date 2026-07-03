using MediatR;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.GetPatientById;

public class GetPatientByIdHandler(IPatientReadRepository readRepository)
    : IRequestHandler<GetPatientByIdQuery, GetPatientByIdResponse?>
{
    public async Task<GetPatientByIdResponse?> Handle(
        GetPatientByIdQuery query,
        CancellationToken cancellationToken)
    {
        var patient = await readRepository.GetByIdAsync(query.Id, cancellationToken);

        if (patient is null)
            return null;

        return new GetPatientByIdResponse(
            patient.Id,
            patient.FullName.Display,
            patient.NationalId.Value,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.Status.ToString(),
            patient.Notes,
            patient.CreatedAt,
            patient.UpdatedAt,
            patient.Contacts.Select(c => new PatientContactResponse(
                c.Id, c.Type.ToString(), c.PhoneNumber, c.Email, c.IsPrimary)),
            patient.Addresses.Select(a => new PatientAddressResponse(
                a.Id, a.Type.ToString(), a.Street, a.Suburb, a.City, a.Country, a.IsPrimary)));
    }
}
