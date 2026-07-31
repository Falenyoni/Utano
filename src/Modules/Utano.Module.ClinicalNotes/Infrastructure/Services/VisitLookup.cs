using Microsoft.EntityFrameworkCore;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Infrastructure.Services;

public class VisitLookup(ClinicalNotesDbContext context) : IVisitLookup
{
    public async Task<IReadOnlyDictionary<Guid, Guid>> GetVisitIdsForAppointmentsAsync(
        IEnumerable<Guid> appointmentIds, CancellationToken cancellationToken = default)
    {
        var ids = appointmentIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, Guid>();

        return await context.Visits
            .AsNoTracking()
            .Where(v => v.AppointmentId != null && ids.Contains(v.AppointmentId!.Value))
            .ToDictionaryAsync(v => v.AppointmentId!.Value, v => v.Id, cancellationToken);
    }
}
