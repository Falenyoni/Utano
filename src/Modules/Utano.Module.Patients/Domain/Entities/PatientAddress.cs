using Utano.Module.Core.Exceptions;
using Utano.Module.Patients.Domain.Enums;

namespace Utano.Module.Patients.Domain.Entities;

public class PatientAddress
{
    private PatientAddress() { }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public AddressType Type { get; private set; }
    public string Street { get; private set; } = null!;
    public string? Suburb { get; private set; }
    public string City { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public bool IsPrimary { get; private set; }

    internal static PatientAddress Create(Guid patientId, AddressType type, string street,
        string city, string country, string? suburb, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new UtanoDomainException("Street is required.");

        if (string.IsNullOrWhiteSpace(city))
            throw new UtanoDomainException("City is required.");

        if (string.IsNullOrWhiteSpace(country))
            throw new UtanoDomainException("Country is required.");

        return new PatientAddress
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Type = type,
            Street = street.Trim(),
            Suburb = suburb?.Trim(),
            City = city.Trim(),
            Country = country.Trim(),
            IsPrimary = isPrimary
        };
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    internal void Update(AddressType type, string street, string city, string country, string? suburb)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new UtanoDomainException("Street is required.");
        if (string.IsNullOrWhiteSpace(city)) throw new UtanoDomainException("City is required.");
        if (string.IsNullOrWhiteSpace(country)) throw new UtanoDomainException("Country is required.");
        Type = type;
        Street = street.Trim();
        City = city.Trim();
        Country = country.Trim();
        Suburb = suburb?.Trim();
    }
}
