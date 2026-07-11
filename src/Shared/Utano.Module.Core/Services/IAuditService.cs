namespace Utano.Module.Core.Services;

public interface IAuditService
{
    Task LogAsync(string entityType, string entityId, string action, string? description = null, CancellationToken ct = default);
}
