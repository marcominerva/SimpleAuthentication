﻿using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication.Swagger;

internal class AuthenticationOperationFilter : IOperationFilter
{
    private readonly IAuthorizationPolicyProvider authorizationPolicyProvider;
    private readonly JwtBearerSettings jwtBearerSettings;
    private readonly ApiKeySettings apiKeySettings;
    private readonly BasicAuthenticationSettings basicAuthenticationSettings;

    public AuthenticationOperationFilter(IAuthorizationPolicyProvider authorizationPolicyProvider,
        IOptions<JwtBearerSettings> jwtBearerSettingsOptions, IOptions<ApiKeySettings> apiKeySettingsOptions, IOptions<BasicAuthenticationSettings> basicAuthenticationSettingsOptions)
    {
        this.authorizationPolicyProvider = authorizationPolicyProvider;

        jwtBearerSettings = jwtBearerSettingsOptions.Value;
        apiKeySettings = apiKeySettingsOptions.Value;
        basicAuthenticationSettings = basicAuthenticationSettingsOptions.Value;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // If the method requires authorization, automatically add 401 and 403 response (if not explicitly specified).
        var fallbackPolicy = authorizationPolicyProvider.GetFallbackPolicyAsync().GetAwaiter().GetResult();
        var requireAuthenticatedUser = fallbackPolicy?.Requirements.Any(r => r is DenyAnonymousAuthorizationRequirement) ?? false;

        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var requireAuthorization = endpointMetadata.Any(m => m is AuthorizeAttribute);
        var allowAnonymous = endpointMetadata.Any(m => m is AllowAnonymousAttribute);

        if ((requireAuthenticatedUser || requireAuthorization) && !allowAnonymous)
        {
            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), Helpers.CreateResponse(HttpStatusCode.Unauthorized.ToString()));
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), Helpers.CreateResponse(HttpStatusCode.Forbidden.ToString()));
        }
    }
}
