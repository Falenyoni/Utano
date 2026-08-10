using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Admin;

// One-off migration for practices whose logo still lives as a base64 data URL on Practice.LogoBase64
// (pre-R2, see #33 in docs/technical-debt-and-priorities.md). Uploads each to R2 via the Files
// module and sets LogoFileId. Safe to call more than once - already-migrated practices (LogoFileId
// already set) are skipped. LogoBase64 itself is left in place; drop it in a follow-up migration
// once the results have been checked in the app.
[ApiController]
[Route("api/admin/practices")]
public class MigrateBrandingLogosEndpoint(ISender sender) : ControllerBase
{
    [HttpPost("migrate-branding-logos")]
    [ProducesResponseType(typeof(MigrateBrandingLogosResult), (int)HttpStatusCode.OK)]
    [EndpointSummary("One-off: upload any practice's legacy base64 logo to R2 and set LogoFileId")]
    [Tags("Admin")]
    public async Task<IActionResult> Migrate(CancellationToken ct)
        => Ok(await sender.Send(new MigrateBrandingLogosCommand(), ct));
}

public record MigrateBrandingLogosCommand : IRequest<MigrateBrandingLogosResult>;

public record MigrateBrandingLogosResult(int Migrated, int Skipped, int Failed, List<string> Errors);

public partial class MigrateBrandingLogosHandler(IdentityDbContext db, IFileAttachmentLookup fileLookup)
    : IRequestHandler<MigrateBrandingLogosCommand, MigrateBrandingLogosResult>
{
    [GeneratedRegex(@"^data:(?<contentType>[\w/+.-]+);base64,(?<data>.+)$", RegexOptions.Singleline)]
    private static partial Regex DataUrlPattern();

    public async Task<MigrateBrandingLogosResult> Handle(MigrateBrandingLogosCommand _, CancellationToken ct)
    {
        var candidates = await db.Practices
            .Where(p => p.LogoBase64 != null && p.LogoFileId == null)
            .ToListAsync(ct);

        var migrated = 0;
        var skipped = 0;
        var errors = new List<string>();

        foreach (var practice in candidates)
        {
            var match = DataUrlPattern().Match(practice.LogoBase64!);
            if (!match.Success)
            {
                skipped++;
                errors.Add($"{practice.Id}: LogoBase64 is not a recognizable data URL, skipped.");
                continue;
            }

            try
            {
                var contentType = match.Groups["contentType"].Value;
                var bytes = Convert.FromBase64String(match.Groups["data"].Value);
                var ext = contentType switch
                {
                    "image/png" => ".png",
                    "image/jpeg" => ".jpg",
                    "image/webp" => ".webp",
                    "image/svg+xml" => ".svg",
                    "image/gif" => ".gif",
                    _ => ""
                };

                var fileId = await fileLookup.CreateLogoFromBytesAsync(
                    practice.Id, bytes, $"logo{ext}", contentType, ct);

                practice.SetLogoFileIdFromMigration(fileId);
                migrated++;
            }
            catch (Exception ex)
            {
                errors.Add($"{practice.Id}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(ct);

        return new MigrateBrandingLogosResult(migrated, skipped, candidates.Count - migrated - skipped, errors);
    }
}
