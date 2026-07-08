using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Utano.API.Configuration;

public class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any() == true ||
            context.MethodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any();

        if (!hasAuthorize) return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer")] = []
            }
        ];
    }
}
