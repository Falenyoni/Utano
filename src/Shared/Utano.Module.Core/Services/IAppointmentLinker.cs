namespace Utano.Module.Core.Services;

public interface IAppointmentLinker
{
    Task MarkInProgressAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task MarkCompletedAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}
