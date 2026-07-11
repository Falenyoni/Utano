namespace Utano.Module.Core.Services;

public interface IAppointmentLinker
{
    Task MarkInProgressAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    /// <summary>Returns the AppointmentType enum name (e.g. "Consultation") or null if not found.</summary>
    Task<string?> GetAppointmentTypeKeyAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
