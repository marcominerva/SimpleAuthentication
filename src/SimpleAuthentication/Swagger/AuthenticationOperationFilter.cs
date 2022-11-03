using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.JwtBearer;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication.Swagger;

internal class AuthenticationOperationFilter : IOperationFilter
{
    private readonly IAuthorizationPolicyProvider authorizationPolicyProvider;
    private readonly JwtBearerSettings jwtBearerSettings;
    private readonly ApiKeySettings apiKeySettings;

    public AuthenticationOperationFilter(IAuthorizationPolicyProvider authorizationPolicyProvider, IOptions<JwtBearerSettings> jwtBearerSettingsOptions, IOptions<ApiKeySettings> apiKeySettingsOptions)
    {
        this.authorizationPolicyProvider = authorizationPolicyProvider;
        jwtBearerSettings = jwtBearerSettingsOptions.Value;
        apiKeySettings = apiKeySettingsOptions.Value;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fallbackPolicy = authorizationPolicyProvider.GetFallbackPolicyAsync().GetAwaiter().GetResult();
        var requireAuthenticatedUser = fallbackPolicy?.Requirements.Any(r => r is DenyAnonymousAuthorizationRequirement) ?? false;

        var requireAuthorization = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .Any(a => a is AuthorizeAttribute) ?? false;

        var allowAnonymous = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .Any(a => a is AllowAnonymousAttribute) ?? false;

        if ((requireAuthenticatedUser || requireAuthorization) && !allowAnonymous)
        {
            var hasJwtBearerAuthentication = !string.IsNullOrWhiteSpace(jwtBearerSettings.SecurityKey);
            CheckAddSecurityRequirement(operation, hasJwtBearerAuthentication ? jwtBearerSettings.SchemeName : null);

            var hasApiKeyHeaderAuthentication = !string.IsNullOrWhiteSpace(apiKeySettings.HeaderName);
            var hasApiKeyQueryAuthentication = !string.IsNullOrWhiteSpace(apiKeySettings.QueryStringKey);
            CheckAddSecurityRequirement(operation, hasApiKeyHeaderAuthentication ? $"{apiKeySettings.SchemeName} in Header" : null);
            CheckAddSecurityRequirement(operation, hasApiKeyQueryAuthentication ? $"{apiKeySettings.SchemeName} in Query String" : null);

            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), GetResponse(HttpStatusCode.Unauthorized.ToString()));
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), GetResponse(HttpStatusCode.Forbidden.ToString()));
        }
    }

    private static void CheckAddSecurityRequirement(OpenApiOperation operation, string? securityScheme)
    {
        if (string.IsNullOrWhiteSpace(securityScheme))
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = securityScheme
                    }
                },
                Array.Empty<string>()
            }
        });
    }

    private static OpenApiResponse GetResponse(string description)
        => new()
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypeNames.Application.Json] = new()
                {
                    Schema = new()
                    {
                        Reference = new()
                        {
                            Id = nameof(ProblemDetails),
                            Type = ReferenceType.Schema
                        }
                    }
                }
            }
        };
}
