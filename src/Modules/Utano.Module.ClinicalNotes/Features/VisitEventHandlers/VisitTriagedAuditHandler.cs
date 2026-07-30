using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.ClinicalNotes;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.VisitEventHandlers;

public class VisitTriagedAuditHandler(
    IAuditService auditService,
    ILogger<VisitTriagedAuditHandler> logger)
    : INotificationHandler<VisitTriagedEvent>
{
    public async Task Handle(VisitTriagedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            await auditService.LogAsync("Visit", domainEvent.VisitId.ToString(), "Triaged",
                $"Patient: {domainEvent.PatientName}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log visit triage for {VisitId}", domainEvent.VisitId);
        }
    }
}
