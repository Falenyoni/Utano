using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.ClinicalNotes;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.VisitEventHandlers;

public class VisitCompletedAuditHandler(
    IAuditService auditService,
    ILogger<VisitCompletedAuditHandler> logger)
    : INotificationHandler<VisitCompletedEvent>
{
    public async Task Handle(VisitCompletedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            await auditService.LogAsync("Visit", domainEvent.VisitId.ToString(), "Completed",
                $"Patient: {domainEvent.PatientName} · Doctor: {domainEvent.DoctorName}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log visit completion for {VisitId}", domainEvent.VisitId);
        }
    }
}
