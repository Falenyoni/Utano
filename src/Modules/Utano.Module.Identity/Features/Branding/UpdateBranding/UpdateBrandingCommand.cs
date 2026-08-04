using MediatR;
using Utano.Module.Core.Authorization;
using Utano.Module.Identity.Configuration;

namespace Utano.Module.Identity.Features.Branding.UpdateBranding;

public record UpdateBrandingCommand(Guid PracticeId, string? PrimaryColor, string? LogoBase64)
    : IRequest, IRequirePermission
{
    public string Permission => UtanoCoreModuleDescriptor.SettingsBranding;
}
