#if NET9_0

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace SimpleAuthentication.OpenApi;

internal class DefaultResponseDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Checks if the ProblemDetails type is already defined in the document (because there is at least one endpoint that returns it).
        var isProblemDetailsSchemaDefined = context.DescriptionGroups
           .SelectMany(g => g.Items).SelectMany(i => i.SupportedResponseTypes)
           .Any(type => type.Type == typeof(ProblemDetails));

        if (isProblemDetailsSchemaDefined)
        {
            return Task.CompletedTask;
        }

        // Otherwise, define the ProblemDetails schema in the document.
        document.Components ??= new();
        document.Components.Schemas.TryAdd(nameof(ProblemDetails), new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["type"] = new()
                {
                    Type = "string",
                    Nullable = true
                },
                ["title"] = new()
                {
                    Type = "string",
                    Nullable = true
                },
                ["status"] = new()
                {
                    Type = "integer",
                    Format = "int32",
                    Nullable = true
                },
                ["detail"] = new()
                {
                    Type = "string",
                    Nullable = true
                },
                ["instance"] = new()
                {
                    Type = "string",
                    Nullable = true
                }
            }
        });

        return Task.CompletedTask;
    }
}

#elif NET10_0_OR_GREATER

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SimpleAuthentication.OpenApi;

internal class DefaultResponseDocumentTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Generate schema for error responses.
        var errorSchema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
        document.AddComponent(nameof(ProblemDetails), errorSchema);
    }
}

#endif