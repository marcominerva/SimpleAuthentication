#if NET9_0

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;

namespace SimpleAuthentication.OpenApi;

internal class AuthenticationDocumentTransformer(IConfiguration configuration, string sectionName, params IEnumerable<OpenApiSecurityRequirement> additionalSecurityRequirements) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Adds a security definition for each authentication method that has been configured.
        CheckAddJwtBearer(document, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(document, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(document, configuration.GetSection($"{sectionName}:Basic"));

        if (additionalSecurityRequirements?.Any() ?? false)
        {
            // Adds all the other security requirements that have been specified.
            foreach (var securityRequirement in additionalSecurityRequirements)
            {
                AddSecurityRequirement(document, securityRequirement);
            }
        }

        return Task.CompletedTask;

        static void CheckAddJwtBearer(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityScheme(document, settings.SchemeName, SecuritySchemeType.Http, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token");
            AddSecurityRequirement(document, settings.SchemeName);
        }

        static void CheckAddApiKey(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.HeaderName))
            {
                var schemeName = $"{settings.SchemeName}-Header";

                AddSecurityScheme(document, schemeName, SecuritySchemeType.ApiKey, null, ParameterLocation.Header, settings.HeaderName, "Insert the API Key");
                AddSecurityRequirement(document, schemeName);
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                var schemeName = $"{settings.SchemeName}-QueryString";

                AddSecurityScheme(document, schemeName, SecuritySchemeType.ApiKey, null, ParameterLocation.Query, settings.QueryStringKey, "Insert the API Key");
                AddSecurityRequirement(document, schemeName);
            }
        }

        static void CheckAddBasicAuthentication(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityScheme(document, settings.SchemeName, SecuritySchemeType.Http, BasicAuthenticationDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert user name and password");
            AddSecurityRequirement(document, settings.SchemeName);
        }
    }

    private static void AddSecurityScheme(OpenApiDocument document, string name, SecuritySchemeType securitySchemeType, string? scheme, ParameterLocation location, string parameterName, string description)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        document.Components.SecuritySchemes.Add(name, new()
        {
            In = location,
            Name = parameterName,
            Description = description,
            Type = securitySchemeType,
            Scheme = scheme
        });
    }

    internal static void AddSecurityRequirement(OpenApiDocument document, string name)
        => AddSecurityRequirement(document, OpenApiHelpers.CreateSecurityRequirement(name));

    private static void AddSecurityRequirement(OpenApiDocument document, OpenApiSecurityRequirement requirement)
    {
        document.SecurityRequirements ??= [];
        document.SecurityRequirements.Add(requirement);
    }
}

#elif NET10_0_OR_GREATER

using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;

namespace SimpleAuthentication.OpenApi;

internal class AuthenticationDocumentTransformer(IConfiguration configuration, string sectionName, IEnumerable<OpenApiSecurityRequirement> additionalSecurityRequirements, IEnumerable<string> additionalSecurityDefinitionNames) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Adds a security definition for each authentication method that has been configured.
        CheckAddJwtBearer(document, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(document, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(document, configuration.GetSection($"{sectionName}:Basic"));

        if (additionalSecurityRequirements.Any())
        {
            // Adds all the other security requirements that have been specified.
            foreach (var securityRequirement in additionalSecurityRequirements)
            {
                AddSecurityRequirement(document, securityRequirement);
            }
        }

        if (additionalSecurityDefinitionNames.Any())
        {
            // Adds all the other security definitions that have been specified.
            foreach (var definitionName in additionalSecurityDefinitionNames)
            {
                AddSecurityRequirement(document, definitionName);
            }
        }

        return Task.CompletedTask;

        static void CheckAddJwtBearer(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityScheme(document, settings.SchemeName, SecuritySchemeType.Http, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token", "JWT");
            AddSecurityRequirement(document, settings.SchemeName);
        }

        static void CheckAddApiKey(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.HeaderName))
            {
                var schemeName = $"{settings.SchemeName}-Header";

                AddSecurityScheme(document, schemeName, SecuritySchemeType.ApiKey, null, ParameterLocation.Header, settings.HeaderName, "Insert the API Key");
                AddSecurityRequirement(document, schemeName);
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                var schemeName = $"{settings.SchemeName}-QueryString";

                AddSecurityScheme(document, schemeName, SecuritySchemeType.ApiKey, null, ParameterLocation.Query, settings.QueryStringKey, "Insert the API Key");
                AddSecurityRequirement(document, schemeName);
            }
        }

        static void CheckAddBasicAuthentication(OpenApiDocument document, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityScheme(document, settings.SchemeName, SecuritySchemeType.Http, BasicAuthenticationDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert user name and password");
            AddSecurityRequirement(document, settings.SchemeName);
        }
    }

    private static void AddSecurityScheme(OpenApiDocument document, string name, SecuritySchemeType securitySchemeType, string? scheme, ParameterLocation location, string parameterName, string description, string? bearerFormat = null)
    {
        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes.Add(name, new OpenApiSecurityScheme()
        {
            In = location,
            Name = parameterName,
            Description = description,
            Type = securitySchemeType,
            Scheme = scheme,
            BearerFormat = bearerFormat
        });
    }

    internal static void AddSecurityRequirement(OpenApiDocument document, string name)
        => AddSecurityRequirement(document, OpenApiHelpers.CreateSecurityRequirement(name, document));

    private static void AddSecurityRequirement(OpenApiDocument document, OpenApiSecurityRequirement requirement)
    {
        document.Security ??= [];
        document.Security.Add(requirement);
    }
}

#endif