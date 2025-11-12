using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication.Swagger;

internal class AuthenticationOperationFilter(IAuthorizationPolicyProvider authorizationPolicyProvider) : IOperationAsyncFilter
{
    public async Task ApplyAsync(OpenApiOperation operation, OperationFilterContext context, CancellationToken cancellationToken)
    {
        // If the method requires authorization, automatically add 401 and 403 response (if not explicitly specified).
        var fallbackPolicy = await authorizationPolicyProvider.GetFallbackPolicyAsync();
        var requireAuthenticatedUser = fallbackPolicy?.Requirements.Any(r => r is DenyAnonymousAuthorizationRequirement) ?? false;

        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var requireAuthorization = endpointMetadata.Any(m => m is AuthorizeAttribute);
        var allowAnonymous = endpointMetadata.Any(m => m is AllowAnonymousAttribute);

        if ((requireAuthenticatedUser || requireAuthorization) && !allowAnonymous)
        {
            operation.Responses ??= [];
            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), OpenApiHelpers.CreateResponse(HttpStatusCode.Unauthorized.ToString()));
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), OpenApiHelpers.CreateResponse(HttpStatusCode.Forbidden.ToString()));
        }
    }
}
