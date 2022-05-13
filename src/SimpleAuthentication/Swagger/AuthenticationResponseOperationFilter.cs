using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication.Swagger;

internal class AuthenticationResponseOperationFilter : IOperationFilter
{
    private readonly IAuthorizationPolicyProvider authorizationPolicyProvider;

    public AuthenticationResponseOperationFilter(IAuthorizationPolicyProvider authorizationPolicyProvider)
    {
        this.authorizationPolicyProvider = authorizationPolicyProvider;
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
            operation.Responses.TryAdd(StatusCodes.Status401Unauthorized.ToString(), GetResponse(HttpStatusCode.Unauthorized.ToString()));
            operation.Responses.TryAdd(StatusCodes.Status403Forbidden.ToString(), GetResponse(HttpStatusCode.Forbidden.ToString()));
        }
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
