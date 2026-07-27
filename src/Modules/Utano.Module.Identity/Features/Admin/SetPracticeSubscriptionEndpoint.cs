using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Utano.Module.Core.Modules;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Entities;
using Utano.Module.Identity.Domain.Interfaces;

namespace Utano.Module.Identity.Features.Admin;

[ApiController]
[Route("api/admin/practices")]
public class SetPracticeSubscriptionEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("{practiceId:guid}/subscription")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [EndpointSummary("Set the subscription tier and status for a practice")]
    [Tags("Admin")]
    public async Task<IActionResult> Set(
        Guid practiceId,
        [FromBody] SetSubscriptionBody body,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new SetPracticeSubscriptionCommand(practiceId, body.Tier, body.Status, body.ExpiresAt), ct);

        return result switch
        {
            SetSubscriptionResult.NotFound => NotFound(),
            SetSubscriptionResult.BadTier => BadRequest("Invalid subscription tier."),
            SetSubscriptionResult.BadStatus => BadRequest("Invalid subscription status."),
            _ => NoContent()
        };
    }
}

public record SetSubscriptionBody(string Tier, string Status, DateTimeOffset? ExpiresAt);

public enum SetSubscriptionResult
{ Ok, NotFound, BadTier, BadStatus }

public record SetPracticeSubscriptionCommand(
    Guid PracticeId, string Tier, string Status, DateTimeOffset? ExpiresAt)
    : IRequest<SetSubscriptionResult>;

public class SetPracticeSubscriptionHandler(
    IPracticeRepository practiceRepository,
    IdentityDbContext db,
    IEnumerable<IModuleDescriptor> moduleDescriptors)
    : IRequestHandler<SetPracticeSubscriptionCommand, SetSubscriptionResult>
{
    private static readonly string[] ValidTiers =
        [SubscriptionTier.Starter, SubscriptionTier.Professional];

    private static readonly string[] ValidStatuses =
        [SubscriptionStatus.Trial, SubscriptionStatus.Active,
         SubscriptionStatus.PastDue, SubscriptionStatus.Cancelled];

    public async Task<SetSubscriptionResult> Handle(
        SetPracticeSubscriptionCommand cmd, CancellationToken ct)
    {
        if (!ValidTiers.Contains(cmd.Tier))
            return SetSubscriptionResult.BadTier;

        if (!ValidStatuses.Contains(cmd.Status))
            return SetSubscriptionResult.BadStatus;

        var practice = await practiceRepository.GetByIdAsync(cmd.PracticeId, ct);
        if (practice is null)
            return SetSubscriptionResult.NotFound;

        practice.SetSubscription(cmd.Tier, cmd.Status, cmd.ExpiresAt);
        await practiceRepository.UpdateAsync(practice, ct);

        await ApplyTierFeaturesAsync(cmd.PracticeId, cmd.Tier, ct);

        return SetSubscriptionResult.Ok;
    }

    private async Task ApplyTierFeaturesAsync(Guid practiceId, string tier, CancellationToken ct)
    {
        var allowedPlans = tier == SubscriptionTier.Professional
            ? new[] { "free", "professional" }
            : new[] { "free" };

        var enabledKeys = moduleDescriptors
            .Where(m => allowedPlans.Contains(m.Plan))
            .Select(m => m.FeatureKey)
            .Distinct()
            .ToHashSet();

        var allKeys = moduleDescriptors
            .Select(m => m.FeatureKey)
            .Distinct()
            .ToHashSet();

        var existing = await db.PracticeFeatures
            .Where(pf => pf.PracticeId == practiceId)
            .ToListAsync(ct);

        var existingKeys = existing.Select(pf => pf.FeatureKey).ToHashSet();

        // Enable/disable existing rows
        foreach (var feature in existing)
        {
            if (enabledKeys.Contains(feature.FeatureKey))
                feature.Enable();
            else
                feature.Disable();
        }

        // Add any missing rows
        var missingKeys = allKeys.Except(existingKeys);
        foreach (var key in missingKeys)
        {
            var pf = PracticeFeature.Create(practiceId, key);
            if (!enabledKeys.Contains(key))
                pf.Disable();
            db.PracticeFeatures.Add(pf);
        }

        await db.SaveChangesAsync(ct);
    }
}