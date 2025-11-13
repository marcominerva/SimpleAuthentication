#if NET9_0

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace SimpleAuthentication.OpenApi;

internal static class OpenApiHelpers
{
    public static OpenApiSecurityRequirement CreateSecurityRequirement(string name)
        => new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = name
                        }
                    },
                    []
                }
            };

    public static OpenApiResponse CreateResponse(string description)
        => new()
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypeNames.Application.ProblemJson] = new()
                {
                    Schema = new()
                    {
                        Reference = new()
                        {
                            Type = ReferenceType.Schema,
                            Id = nameof(ProblemDetails)
                        }
                    }
                }
            }
        };
}

#elif NET10_0_OR_GREATER

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace SimpleAuthentication.OpenApi;

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