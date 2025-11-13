using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace SimpleAuthentication.Swagger;

internal static class OpenApiHelpers
{
    public static OpenApiSecurityRequirement CreateSecurityRequirement(string name, OpenApiDocument document)
        => new()
            {
                { new OpenApiSecuritySchemeReference(name, document), [] }
            };

    public static OpenApiResponse CreateResponse(string description)
        => new()
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypeNames.Application.ProblemJson] = new()
                {
                    Schema = new OpenApiSchemaReference(nameof(ProblemDetails))
                }
            }
        };
}
