using MediatR;
using Utano.Module.Core.Authorization;
using Utano.Module.Identity.Configuration;

namespace Utano.Module.Identity.Features.Branding.UpdateBranding;

public record UpdateBrandingCommand(Guid PracticeId, string? PrimaryColor, Guid? LogoFileId, bool ClearLogo)
    : IRequest, IRequirePermission
{
    public string Permission => UtanoCoreModuleDescriptor.SettingsBranding;
}
