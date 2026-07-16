using MediatR;

namespace Utano.Module.Identity.Features.Branding.GetBranding;

public record GetBrandingQuery(Guid PracticeId) : IRequest<BrandingDto>;

public record BrandingDto(string PracticeName, string? PrimaryColor, string? LogoBase64);
