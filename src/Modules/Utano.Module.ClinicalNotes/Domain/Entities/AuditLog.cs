namespace Utano.Module.ClinicalNotes.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid PracticeId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
