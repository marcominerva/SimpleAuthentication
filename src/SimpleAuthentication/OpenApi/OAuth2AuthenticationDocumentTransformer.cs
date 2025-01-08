#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SimpleAuthentication.OpenApi;

internal class OAuth2AuthenticationDocumentTransformer(string name, OpenApiOAuthFlow authFlow) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes.Add(name, new()
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new()
            {
                Implicit = authFlow
            }
        });

        AuthenticationDocumentTransformer.AddSecurityRequirement(document, name);

        return Task.CompletedTask;
    }
}

#endif