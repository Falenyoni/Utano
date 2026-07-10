using MediatR;
using Utano.Module.Patients.Domain.Interfaces;

namespace Utano.Module.Patients.Features.Patients.GetPatientById;

public class GetPatientByIdHandler(
    IPatientReadRepository readRepository,
    IMedicalAidRepository medicalAidRepository)
    : IRequestHandler<GetPatientByIdQuery, GetPatientByIdResponse?>
{
    public async Task<GetPatientByIdResponse?> Handle(
        GetPatientByIdQuery query,
        CancellationToken cancellationToken)
    {
        var patient = await readRepository.GetByIdAsync(query.Id, cancellationToken);
        if (patient is null) return null;

        string? medicalAidName = null;
        if (patient.MedicalAidId.HasValue)
        {
            var aid = await medicalAidRepository.GetByIdAsync(patient.MedicalAidId.Value, cancellationToken);
            medicalAidName = aid?.Name;
        }

        return new GetPatientByIdResponse(
            patient.Id,
            patient.FullName.Display,
            patient.FullName.FirstName,
            patient.FullName.LastName,
            string.IsNullOrWhiteSpace(patient.FullName.MiddleName) ? null : patient.FullName.MiddleName,
            patient.NationalId.Value,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.Status.ToString(),
            patient.Notes,
            patient.BloodGroup?.ToString(),
            patient.Allergies,
            patient.ChronicConditions,
            patient.MedicalAidId,
            medicalAidName,
            patient.MedicalAidNumber,
            patient.CreatedAt,
            patient.UpdatedAt,
            patient.Contacts.Select(c => new PatientContactResponse(
                c.Id, c.Type.ToString(), c.PhoneNumber, c.Email, c.IsPrimary)),
            patient.Addresses.Select(a => new PatientAddressResponse(
                a.Id, a.Type.ToString(), a.Street, a.Suburb, a.City, a.Country, a.IsPrimary)));
    }
}
