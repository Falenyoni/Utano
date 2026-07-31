namespace Utano.Module.Core.Services;

public interface IVisitLookup
{
    /// <summary>Maps AppointmentId -> VisitId for whichever of the given appointments have a visit.</summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetVisitIdsForAppointmentsAsync(
        IEnumerable<Guid> appointmentIds, CancellationToken cancellationToken = default);
}
