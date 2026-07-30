using Utano.Module.Core.Domain.Events;

namespace Utano.Module.Core.Domain.Aggregate;

public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
