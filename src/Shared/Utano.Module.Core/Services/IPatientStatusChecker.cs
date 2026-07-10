namespace Utano.Module.Core.Services;

public interface IPatientStatusChecker
{
    Task<bool> IsActiveAsync(Guid patientId, CancellationToken cancellationToken = default);
}
