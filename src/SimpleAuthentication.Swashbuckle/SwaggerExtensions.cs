using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;
using SimpleAuthentication.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding authentication support in Swagger.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, string sectionName = "Authentication")
        => options.AddSimpleAuthentication(configuration, sectionName, Array.Empty<OpenApiSecurityRequirement>());

    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from a section named <strong>Authentication</strong> in <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="additionalSecurityDefinitionNames">The name of additional security definitions that have been defined in Swagger.</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, IEnumerable<string>? additionalSecurityDefinitionNames)
        => options.AddSimpleAuthentication(configuration, "Authentication", additionalSecurityDefinitionNames);

    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="additionalSecurityDefinitionNames">The name of additional security definitions that have been defined in Swagger.</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, string sectionName, IEnumerable<string>? additionalSecurityDefinitionNames)
    {
        var securityRequirements = additionalSecurityDefinitionNames?.Select(OpenApiHelpers.CreateSecurityRequirement).ToArray();
        options.AddSimpleAuthentication(configuration, sectionName, securityRequirements ?? []);
    }

    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="securityRequirements">Additional security requirements to be added to Swagger definition.</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, params OpenApiSecurityRequirement[] securityRequirements)
        => options.AddSimpleAuthentication(configuration, "Authentication", securityRequirements);

    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="additionalSecurityRequirements">Additional security requirements to be added to Swagger definition.</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, string sectionName, params OpenApiSecurityRequirement[] additionalSecurityRequirements)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        // Adds a security definition for each authentication method that has been configured.
        CheckAddJwtBearer(options, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(options, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(options, configuration.GetSection($"{sectionName}:Basic"));

        if (additionalSecurityRequirements?.Any() ?? false)
        {
            // Adds all the other security requirements that have been specified.
            foreach (var securityRequirement in additionalSecurityRequirements)
            {
                options.AddSecurityRequirement(securityRequirement);
            }
        }

        options.OperationAsyncFilter<AuthenticationOperationFilter>();
        options.DocumentFilter<ProblemDetailsDocumentFilter>();

        static void CheckAddJwtBearer(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityDefinition(options, settings.SchemeName, SecuritySchemeType.Http, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token");
            AddSecurityRequirement(options, settings.SchemeName);
        }

        static void CheckAddApiKey(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.HeaderName))
            {
                AddSecurityDefinition(options, $"{settings.SchemeName} in Header", SecuritySchemeType.ApiKey, null, ParameterLocation.Header, settings.HeaderName, "Insert the API Key");
                AddSecurityRequirement(options, $"{settings.SchemeName} in Header");
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                AddSecurityDefinition(options, $"{settings.SchemeName} in Query String", SecuritySchemeType.ApiKey, null, ParameterLocation.Query, settings.QueryStringKey, "Insert the API Key");
                AddSecurityRequirement(options, $"{settings.SchemeName} in Query String");
            }
        }

        static void CheckAddBasicAuthentication(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            AddSecurityDefinition(options, settings.SchemeName, SecuritySchemeType.Http, BasicAuthenticationDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert user name and password");
            AddSecurityRequirement(options, settings.SchemeName);
        }

        static void AddSecurityDefinition(SwaggerGenOptions options, string name, SecuritySchemeType securitySchemeType, string? scheme, ParameterLocation location, string parameterName, string description)
            => options.AddSecurityDefinition(name, new()
            {
                In = location,
                Name = parameterName,
                Description = description,
                Type = securitySchemeType,
                Scheme = scheme
            });
    }

    /// <summary>
    /// Adds OAuth2 authentication support in Swagger, for example for example to integrate with Microsoft.Identity.Web.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="name">The name of the OAuth2 authentication scheme to add.</param>
    /// <param name="authorizationUrl">The Authorization Url for OAuth2 authorization.</param>
    /// <param name="tokenUrl">The Token Url for OAuth2 authorization.</param>
    /// <param name="scopes">The list of scopes.</param>
    /// <seealso cref="SwaggerGenOptions"/>
    public static void AddOAuth2Authentication(this SwaggerGenOptions options, string name, string authorizationUrl, string tokenUrl, IDictionary<string, string> scopes)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenUrl);
        ArgumentNullException.ThrowIfNull(scopes);

        options.AddOAuth2Authentication(name, new()
        {
            AuthorizationUrl = new(authorizationUrl),
            TokenUrl = new(tokenUrl),
            Scopes = scopes
        });
    }

    /// <summary>
    /// Adds OAuth2 authentication support in Open API, for example for example to integrate with Microsoft.Identity.Web.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="name">The name of the OAuth2 authentication scheme to add.</param>
    /// <param name="authFlow">The <see cref="OpenApiOAuthFlow">object</see> that describes the authorization flow.</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="OpenApiOAuthFlow"/>
    public static void AddOAuth2Authentication(this SwaggerGenOptions options, string name, OpenApiOAuthFlow authFlow)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(authFlow);

        options.AddSecurityDefinition(name, new()
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new()
            {
                Implicit = authFlow
            }
        });

        AddSecurityRequirement(options, name);
    }

    private static void AddSecurityRequirement(SwaggerGenOptions options, string name)
        => options.AddSecurityRequirement(OpenApiHelpers.CreateSecurityRequirement(name));
}
