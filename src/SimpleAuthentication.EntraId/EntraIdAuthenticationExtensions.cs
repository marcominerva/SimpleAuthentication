using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using SimpleAuthentication.EntraId;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding Microsoft Entra ID authentication support in ASP.NET Core.
/// </summary>
/// <seealso cref="AuthenticationBuilder"/>
public static class EntraIdAuthenticationExtensions
{
    /// <summary>
    /// Registers a named JWT Bearer authentication scheme that validates access tokens issued by a single Microsoft Entra ID tenant,
    /// reading configuration from the specified <see cref="IConfigurationSection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/> to add the scheme to.</param>
    /// <param name="configurationSection">
    /// The <see cref="IConfigurationSection"/> that holds Microsoft Entra ID settings, in the format used by Microsoft.Identity.Web
    /// (<c>Instance</c>, <c>TenantId</c>, <c>ClientId</c> and, when the application needs to call other APIs, <c>ClientCredentials</c>).
    /// </param>
    /// <param name="schemeName">The name of the authentication scheme to register (default: EntraId).</param>
    /// <returns>
    /// A <see cref="MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration"/> that can be used to enable token acquisition,
    /// for example calling <c>EnableTokenAcquisitionToCallDownstreamApi()</c> followed by the registration of a token cache.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configurationSection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="schemeName"/> is empty, or <c>TenantId</c> or <c>ClientId</c> is not a GUID.</exception>
    /// <remarks>
    /// <para>
    /// The scheme is added next to the ones that are already registered, without changing the default scheme and without adding a fallback policy,
    /// so every endpoint that must accept Microsoft Entra ID tokens has to opt in explicitly, for example using
    /// <see cref="EntraIdAuthorizationExtensions.RequireEntraIdApplicationRole(Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder, string[])"/>.
    /// If the application has a single scheme, which ASP.NET Core automatically uses as the default one, that scheme remains the default one.
    /// </para>
    /// <para>
    /// Token validation is delegated to Microsoft.Identity.Web. Signature, issuer, audience and lifetime are always validated, even if the
    /// configuration tries to disable these checks, and tokens whose <c>tid</c> claim does not match the configured tenant are rejected.
    /// </para>
    /// <para>The configuration is validated when the application starts, so that an invalid setup is reported immediately instead of at the first request.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddSimpleAuthentication(builder.Configuration)
    ///     .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"))
    ///     .EnableTokenAcquisitionToCallDownstreamApi()
    ///     .AddInMemoryTokenCaches();
    /// </code>
    /// </example>
    /// <seealso cref="AuthenticationBuilder"/>
    /// <seealso cref="IConfigurationSection"/>
    /// <seealso cref="EntraIdDefaults"/>
    [RequiresUnreferencedCode("Microsoft Entra ID settings are bound from configuration using reflection.")]
    [RequiresDynamicCode("Microsoft Entra ID settings are bound from configuration using reflection.")]
    public static MicrosoftIdentityWebApiAuthenticationBuilderWithConfiguration AddEntraIdJwtBearer(this AuthenticationBuilder builder, IConfigurationSection configurationSection, string schemeName = EntraIdDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationSection);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);

        var tenantId = GetIdentifier(configurationSection, "TenantId", "Only single-tenant applications are supported: values such as 'common' or 'organizations' are not allowed.");
        GetIdentifier(configurationSection, "ClientId", "Use the application (client) ID of the app registration that represents the API.");

        var identityBuilder = builder.AddMicrosoftIdentityWebApi(configurationSection, schemeName);

        builder.Services.Configure<EntraIdSchemeOptions>(options => options.SchemeNames.Add(schemeName));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<AuthenticationOptions>, EntraIdDefaultSchemePostConfigureOptions>());

        builder.Services.AddOptions<JwtBearerOptions>(schemeName)
            .PostConfigure(options =>
            {
                var tokenValidationParameters = options.TokenValidationParameters;

                // Identities created by this scheme must be recognizable, so that application role requirements can ignore
                // claims coming from the other authentication schemes.
                tokenValidationParameters.AuthenticationType = schemeName;

                tokenValidationParameters.RequireSignedTokens = true;
                tokenValidationParameters.RequireExpirationTime = true;
                tokenValidationParameters.ValidateIssuer = true;
                tokenValidationParameters.ValidateAudience = true;
                tokenValidationParameters.ValidateLifetime = true;

                options.Events ??= new();

                var onTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = context =>
                {
                    // Issuer validation already binds tokens to the tenant. This check keeps the scheme single-tenant
                    // even if issuer validation has been customized.
                    if (!string.Equals(context.Principal?.GetTenantId(), tenantId, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Fail("The token has not been issued by the configured Microsoft Entra ID tenant.");
                        return Task.CompletedTask;
                    }

                    return onTokenValidated(context);
                };
            })
            .ValidateOnStart();

        return identityBuilder;
    }

    private static string GetIdentifier(IConfigurationSection configurationSection, string key, string hint)
    {
        var value = configurationSection[key];
        if (!Guid.TryParseExact(value, "D", out _))
        {
            throw new ArgumentException($"The configuration value '{configurationSection.Path}:{key}' is required and must be a GUID. {hint}", key);
        }

        return value;
    }
}
