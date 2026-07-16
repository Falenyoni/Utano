using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Branding.UpdateBranding;

public class UpdateBrandingHandler(IdentityDbContext db) : IRequestHandler<UpdateBrandingCommand>
{
    public async Task Handle(UpdateBrandingCommand cmd, CancellationToken ct)
    {
        var practice = await db.Practices.FirstOrDefaultAsync(p => p.Id == cmd.PracticeId, ct)
            ?? throw new UtanoDomainException("Practice not found.");

        practice.UpdateBranding(cmd.PrimaryColor, cmd.LogoBase64);
        await db.SaveChangesAsync(ct);
    }
}
