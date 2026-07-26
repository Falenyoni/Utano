namespace Utano.Module.Core.Services;

public interface IFeatureService
{
    Task<bool> IsEnabledAsync(Guid practiceId, string featureKey, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEnabledFeaturesAsync(Guid practiceId, CancellationToken ct = default);
}
