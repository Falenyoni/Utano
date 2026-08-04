using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;

namespace Utano.Module.Core.Authorization;

// Gates access to an entire module based on subscription tier, using the module's own declared
// Plan (IModuleDescriptor.Plan) - no per-command opt-in needed. A request is matched to its owning
// module by comparing assemblies: every module's commands and its IModuleDescriptor
// implementation live in the same assembly by construction, so this works without each module
// having to declare a feature key on every single command the way IRequirePermission does.
public class SubscriptionTierBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IEnumerable<IModuleDescriptor> moduleDescriptors)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string ProfessionalTier = "Professional";
    private const string ProfessionalPlan = "professional";

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Empty means an old token issued before this claim existed - let it through rather than
        // lock someone out of features they already had until their next login/refresh, the same
        // graceful-degradation SubscriptionMiddleware already uses for SubscriptionStatus.
        if (string.IsNullOrEmpty(currentUser.SubscriptionTier))
            return next();

        var requestAssembly = typeof(TRequest).Assembly;
        var owningModule = moduleDescriptors.FirstOrDefault(m => m.GetType().Assembly == requestAssembly);

        var requiresProfessional = owningModule?.Plan == ProfessionalPlan;
        var hasProfessional = string.Equals(currentUser.SubscriptionTier, ProfessionalTier, StringComparison.OrdinalIgnoreCase);

        if (requiresProfessional && !hasProfessional)
        {
            throw new UtanoForbiddenException(
                "This feature requires a Professional subscription. Ask your practice administrator to upgrade.");
        }

        return next();
    }
}
