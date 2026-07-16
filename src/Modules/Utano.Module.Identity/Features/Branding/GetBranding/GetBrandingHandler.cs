using MediatR;
using Microsoft.EntityFrameworkCore;
using Utano.Module.Core.Exceptions;
using Utano.Module.Identity.DatabaseMappings;

namespace Utano.Module.Identity.Features.Branding.GetBranding;

public class GetBrandingHandler(IdentityDbContext db) : IRequestHandler<GetBrandingQuery, BrandingDto>
{
    public async Task<BrandingDto> Handle(GetBrandingQuery request, CancellationToken ct)
    {
        var practice = await db.Practices.FirstOrDefaultAsync(p => p.Id == request.PracticeId, ct)
            ?? throw new UtanoDomainException("Practice not found.");

        return new BrandingDto(practice.Name, practice.PrimaryColor, practice.LogoBase64);
    }
}
