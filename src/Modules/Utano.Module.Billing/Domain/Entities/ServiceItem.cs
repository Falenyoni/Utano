using Utano.Module.Billing.Domain.Enums;

namespace Utano.Module.Billing.Domain.Entities;

/// <summary>
/// Practice service price list — consultations, procedures, and other billable items.
/// PracticeId = null means a global/system item visible to all practices.
/// </summary>
public class ServiceItem
{
    private ServiceItem() { }

    public Guid Id { get; private set; }
    public Guid? PracticeId { get; private set; }
    public string Name { get; private set; } = null!;
    public ServiceItemCategory Category { get; private set; }
    public string? NhrplCode { get; private set; }
    public string? DefaultIcdCode { get; private set; }
    public decimal DefaultPrice { get; private set; }
    // Matches AppointmentType enum name — used to auto-add consultation fee when visit is opened
    public string? AppointmentTypeKey { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ServiceItem Create(
        Guid practiceId,
        string name,
        ServiceItemCategory category,
        string? nhrplCode,
        string? defaultIcdCode,
        decimal defaultPrice,
        string? appointmentTypeKey = null)
    {
        return new ServiceItem
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            Name = name.Trim(),
            Category = category,
            NhrplCode = nhrplCode?.Trim(),
            DefaultIcdCode = defaultIcdCode?.Trim(),
            DefaultPrice = defaultPrice,
            AppointmentTypeKey = appointmentTypeKey,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string name,
        string? nhrplCode,
        string? defaultIcdCode,
        decimal defaultPrice,
        string? appointmentTypeKey)
    {
        Name = name.Trim();
        NhrplCode = nhrplCode?.Trim();
        DefaultIcdCode = defaultIcdCode?.Trim();
        DefaultPrice = defaultPrice;
        AppointmentTypeKey = appointmentTypeKey;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTimeOffset.UtcNow; }
}
