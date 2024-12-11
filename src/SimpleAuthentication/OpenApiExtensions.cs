#if NET9_0_OR_GREATER

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;
using SimpleAuthentication.OpenApi;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding authentication support in Open API.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds authentication support in Open API, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName = "Authentication")
        => options.AddSimpleAuthentication(configuration, sectionName, Array.Empty<OpenApiSecurityRequirement>());

    /// <summary>
    /// Adds authentication support in Open API, reading configuration from a section named <strong>Authentication</strong> in <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="additionalSecurityDefinitionNames">The name of additional security definitions that have been defined in Swagger.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, params IEnumerable<string> additionalSecurityDefinitionNames)
        => options.AddSimpleAuthentication(configuration, "Authentication", additionalSecurityDefinitionNames);

    /// <summary>
    /// Adds authentication support in Open API, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="additionalSecurityDefinitionNames">The name of additional security definitions that have been defined in Swagger.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, string sectionName, params IEnumerable<string> additionalSecurityDefinitionNames)
    {
        var securityRequirements = additionalSecurityDefinitionNames?.Select(Helpers.CreateSecurityRequirement).ToArray();
        options.AddSimpleAuthentication(configuration, sectionName, securityRequirements ?? []);
    }

    /// <summary>
    /// Adds authentication support in Open API, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="securityRequirements">Additional security requirements to be added to Swagger definition.</param>
    /// <seealso cref="OpenApiOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this OpenApiOptions options, IConfiguration configuration, params IEnumerable<OpenApiSecurityRequirement> securityRequirements)
        => options.AddSimpleAuthentication(configuration, "Authentication", securityRequirements);

    /// <summary>
    /// Adds authentication support in Open API, reading configuration from the specified <see cref="IConfiguration"/> source.
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
}

#endif