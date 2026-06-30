namespace Utano.Module.Core.Domain.Aggregate;

public abstract class AggregateRoot : IAggregateRoot
{
    public Guid Id { get; protected set; }
    public Guid PracticeId { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }
}
