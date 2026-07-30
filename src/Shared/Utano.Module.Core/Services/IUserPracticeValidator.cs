namespace Utano.Module.Core.Services;

public interface IUserPracticeValidator
{
    Task<bool> IsUserInPracticeAsync(Guid userId, Guid practiceId, CancellationToken cancellationToken = default);
}
