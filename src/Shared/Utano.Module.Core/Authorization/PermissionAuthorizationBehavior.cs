using MediatR;
using Utano.Module.Core.Exceptions;
using Utano.Module.Core.Services;

namespace Utano.Module.Core.Authorization;

// Registered once in Utano.API against the open generic IPipelineBehavior<,> - applies to every
// request across every module's MediatR registration, since they all share one DI container.
// Only requests that opt in via IRequirePermission are checked; everything else passes through
// unchanged.
public class PermissionAuthorizationBehavior<TRequest, TResponse>(ICurrentUserService currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequirePermission requiresPermission
            && !currentUser.HasPermission(requiresPermission.Permission))
        {
            throw new UtanoForbiddenException(
                $"You do not have permission to perform this action. Required permission: {requiresPermission.Permission}");
        }

        return next();
    }
}
