#if NET9_0_OR_GREATER

using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SimpleAuthentication.OpenApi;

internal class AuthenticationOperationTransformer(IAuthorizationPolicyProvider authorizationPolicyProvider) : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // If the method requires authorization, automatically add 401 and 403 response (if not explicitly specified).
        var fallbackPolicy = await authorizationPolicyProvider.GetFallbackPolicyAsync();
        var requireAuthenticatedUser = fallbackPolicy?.Requirements.Any(r => r is DenyAnonymousAuthorizationRequirement) ?? false;

        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;

        var requireAuthorization = endpointMetadata.Any(m => m is AuthorizeAttribute);
        var allowAnonymous = endpointMetadata.Any(m => m is AllowAnonymousAttribute);

        if ((requireAuthenticatedUser || requireAuthorization) && !allowAnonymous)
        {
            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), Helpers.CreateResponse(HttpStatusCode.Unauthorized.ToString()));
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), Helpers.CreateResponse(HttpStatusCode.Forbidden.ToString()));
        }
    }
}

#endif