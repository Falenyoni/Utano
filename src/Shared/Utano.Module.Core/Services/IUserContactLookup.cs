namespace Utano.Module.Core.Services;

public interface IUserContactLookup
{
    Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken = default);
}
