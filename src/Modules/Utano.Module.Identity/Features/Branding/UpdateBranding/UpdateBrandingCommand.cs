using MediatR;

namespace Utano.Module.Identity.Features.Branding.UpdateBranding;

public record UpdateBrandingCommand(Guid PracticeId, string? PrimaryColor, string? LogoBase64) : IRequest;
