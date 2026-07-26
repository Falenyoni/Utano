namespace Utano.Module.Identity.Domain.Entities;

public class PracticeFeature
{
    public Guid Id { get; private set; }
    public Guid PracticeId { get; private set; }
    public string FeatureKey { get; private set; } = null!;
    public bool IsEnabled { get; private set; }
    public DateTimeOffset EnabledAt { get; private set; }

    private PracticeFeature() { }

    public static PracticeFeature Create(Guid practiceId, string featureKey) => new()
    {
        Id = Guid.NewGuid(),
        PracticeId = practiceId,
        FeatureKey = featureKey,
        IsEnabled = true,
        EnabledAt = DateTimeOffset.UtcNow
    };

    public void Disable() => IsEnabled = false;
    public void Enable() { IsEnabled = true; EnabledAt = DateTimeOffset.UtcNow; }
}
