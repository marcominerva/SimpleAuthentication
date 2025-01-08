#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.OpenApi;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding authentication support in OpenAPI.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds authentication support in OpenAPI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName = "Authentication")
        => options.AddSimpleAuthentication(configuration, sectionName, Array.Empty<OpenApiSecurityRequirement>());

    /// <summary>
    /// Adds authentication support in OpenAPI, reading configuration from a section named <strong>Authentication</strong> in <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="additionalSecurityDefinitionNames">The name of additional security definitions that have been defined in Swagger.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, params IEnumerable<string> additionalSecurityDefinitionNames)
        => options.AddSimpleAuthentication(configuration, "Authentication", additionalSecurityDefinitionNames);

    /// <summary>
    /// Adds authentication support in OpenAPI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="additionalSecurityDefinitionNames">The name of additional security definitions that have been defined in Swagger.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName, params IEnumerable<string> additionalSecurityDefinitionNames)
    {
        var securityRequirements = additionalSecurityDefinitionNames?.Select(OpenApiHelpers.CreateSecurityRequirement).ToArray();
        options.AddSimpleAuthentication(configuration, sectionName, securityRequirements ?? []);
    }

    /// <summary>
    /// Adds authentication support in OpenAPI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="securityRequirements">Additional security requirements to be added to Swagger definition.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, params IEnumerable<OpenApiSecurityRequirement> securityRequirements)
        => options.AddSimpleAuthentication(configuration, "Authentication", securityRequirements);

    /// <summary>
    /// Adds authentication support in OpenAPI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="additionalSecurityRequirements">Additional security requirements to be added to Swagger definition.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName, params IEnumerable<OpenApiSecurityRequirement> additionalSecurityRequirements)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        options.AddDocumentTransformer(new AuthenticationDocumentTransformer(configuration, sectionName, additionalSecurityRequirements));
        options.AddOperationTransformer<AuthenticationOperationTransformer>();
    }

    /// <summary>
    /// Adds OAuth2 authentication support in OpenAPI, for example to integrate with Microsoft.Identity.Web.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="name">The name of the OAuth2 authentication scheme to add.</param>
    /// <param name="authorizationUrl">The Authorization Url for OAuth2 authorization.</param>
    /// <param name="tokenUrl">The Token Url for OAuth2 authorization.</param>
    /// <param name="scopes">The list of scopes.</param>
    /// <seealso cref="OpenApiOptions"/>
    public static void AddOAuth2Authentication(this OpenApiOptions options, string name, string authorizationUrl, string tokenUrl, IDictionary<string, string> scopes)
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
    /// Adds OAuth2 authentication support in OpenAPI, for example to integrate with Microsoft.Identity.Web.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="name">The name of the OAuth2 authentication scheme to add.</param>
    /// <param name="authFlow">The <see cref="OpenApiOAuthFlow">object</see> that describes the authorization flow.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="OpenApiOAuthFlow"/>
    public static void AddOAuth2Authentication(this OpenApiOptions options, string name, OpenApiOAuthFlow authFlow)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(authFlow);

        options.AddDocumentTransformer(new OAuth2AuthenticationDocumentTransformer(name, authFlow));
    }
}

#endif