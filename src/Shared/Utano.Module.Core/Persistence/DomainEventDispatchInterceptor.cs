using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Utano.Module.Core.Domain.Aggregate;

namespace Utano.Module.Core.Persistence;

/// <summary>
/// After a DbContext successfully saves, publishes and clears any domain events queued on
/// tracked entities implementing <see cref="IHasDomainEvents"/>. Register on a module's
/// DbContext via <c>options.AddInterceptors(sp.GetRequiredService&lt;DomainEventDispatchInterceptor&gt;())</c>.
/// </summary>
public class DomainEventDispatchInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            var entitiesWithEvents = context.ChangeTracker.Entries()
                .Select(e => e.Entity)
                .OfType<IHasDomainEvents>()
                .Where(e => e.DomainEvents.Count > 0)
                .ToList();

            foreach (var entity in entitiesWithEvents)
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();

                foreach (var domainEvent in events)
                    await publisher.Publish(domainEvent, cancellationToken);
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
