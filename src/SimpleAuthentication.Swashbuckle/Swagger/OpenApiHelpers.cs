#if NET11_0_OR_GREATER

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
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                [MediaTypeNames.Application.ProblemJson] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchemaReference(nameof(ProblemDetails))
                }
            }
        };
}

#else

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

#endif