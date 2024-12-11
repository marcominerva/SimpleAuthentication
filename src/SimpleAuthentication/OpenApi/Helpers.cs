#if NET9_0_OR_GREATER

using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace SimpleAuthentication.OpenApi;

internal static class Helpers
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

#endif