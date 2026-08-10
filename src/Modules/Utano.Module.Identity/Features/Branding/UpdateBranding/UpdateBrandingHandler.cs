using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Branding.UpdateBranding;

public class UpdateBrandingHandler(IdentityDbContext db, IFileAttachmentLookup fileLookup)
    : IRequestHandler<UpdateBrandingCommand>
{
    public async Task Handle(UpdateBrandingCommand cmd, CancellationToken ct)
    {
        var practice = await db.Practices.FirstOrDefaultAsync(p => p.Id == cmd.PracticeId, ct)
            ?? throw new UtanoDomainException("Practice not found.");

        var orphanedLogoFileId = practice.UpdateBranding(cmd.PrimaryColor, cmd.LogoFileId, cmd.ClearLogo);
        await db.SaveChangesAsync(ct);

        if (orphanedLogoFileId is not null)
            await fileLookup.DeleteAsync(orphanedLogoFileId, ct);
    }
}
