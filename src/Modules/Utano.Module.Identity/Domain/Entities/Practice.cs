using Utano.Module.Core.Domain.Aggregate;
using Utano.Module.Core.Exceptions;

namespace Utano.Module.Identity.Domain.Entities;

public class Practice : AggregateRoot
{
    private Practice() { }

    public string Name { get; private set; } = null!;
    public string ContactEmail { get; private set; } = null!;
    public string ContactPhone { get; private set; } = null!;
    public string PhysicalAddress { get; private set; } = null!;
    public bool IsActive { get; private set; }

    public void Update(string name, string contactEmail, string contactPhone, string physicalAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Practice name is required.");
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new UtanoDomainException("Contact email is required.");
        Name = name.Trim();
        ContactEmail = contactEmail.Trim().ToLower();
        ContactPhone = contactPhone.Trim();
        PhysicalAddress = physicalAddress.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Practice Create(string name, string contactEmail,
        string contactPhone, string physicalAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Practice name is required.");

        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new UtanoDomainException("Contact email is required.");

        var id = Guid.NewGuid();

        return new Practice
        {
            Id = id,
            PracticeId = id,
            Name = name.Trim(),
            ContactEmail = contactEmail.Trim().ToLower(),
            ContactPhone = contactPhone.Trim(),
            PhysicalAddress = physicalAddress.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
