using Utano.Module.Core.Exceptions;

namespace Utano.Module.Patients.Domain.Entities;

public class MedicalAid
{
    private MedicalAid() { }

    public Guid Id { get; private set; }
    public Guid PracticeId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static MedicalAid Create(Guid practiceId, string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UtanoDomainException("Medical aid name is required.");
        if (string.IsNullOrWhiteSpace(code))
            throw new UtanoDomainException("Medical aid code is required.");

        return new MedicalAid
        {
            Id = Guid.NewGuid(),
            PracticeId = practiceId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
