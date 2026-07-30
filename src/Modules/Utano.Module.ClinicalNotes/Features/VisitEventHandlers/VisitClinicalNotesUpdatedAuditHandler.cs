using MediatR;
using Microsoft.Extensions.Logging;
using Utano.Module.Core.Domain.Events.ClinicalNotes;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Features.VisitEventHandlers;

public class VisitClinicalNotesUpdatedAuditHandler(
    IAuditService auditService,
    ILogger<VisitClinicalNotesUpdatedAuditHandler> logger)
    : INotificationHandler<VisitClinicalNotesUpdatedEvent>
{
    public async Task Handle(VisitClinicalNotesUpdatedEvent domainEvent, CancellationToken cancellationToken)
    {
        try
        {
            await auditService.LogAsync("Visit", domainEvent.VisitId.ToString(), "ClinicalNotesUpdated",
                $"Patient: {domainEvent.PatientName}", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to audit-log clinical notes update for {VisitId}", domainEvent.VisitId);
        }
    }
}
