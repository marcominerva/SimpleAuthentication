using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace SimpleAuthentication.Swagger;

internal static class Helpers
{
    public static OpenApiSecurityRequirement CreateSecurityRequirement(string name)
    {
        var requirement = new OpenApiSecurityRequirement()
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

        return requirement;
    }

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
