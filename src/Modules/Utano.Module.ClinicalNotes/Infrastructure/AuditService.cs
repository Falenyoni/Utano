using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Entities;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Infrastructure;

public class AuditService(ClinicalNotesDbContext db, ICurrentUserService currentUser) : IAuditService
{
    public async Task LogAsync(string entityType, string entityId, string action, string? description = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            PracticeId = currentUser.PracticeId,
            UserId = currentUser.UserId,
            UserName = currentUser.FullName,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            Timestamp = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
