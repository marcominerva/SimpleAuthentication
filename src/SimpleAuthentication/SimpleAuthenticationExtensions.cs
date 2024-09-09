using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding authentication support in ASP.NET Core.
/// </summary>
/// <seealso cref="AuthenticationBuilder"/>
public static class SimpleAuthenticationExtensions
{
    /// <summary>
    /// Registers services required by authentication services, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="addAuthorizationServices">Set to <see langword="true"/> to automatically add Authorization policy services.</param>
    /// <returns>An <see cref="AuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentException">Configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="IServiceCollection"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="AuthenticationBuilder"/>    
    public static AuthenticationBuilder AddSimpleAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication", bool addAuthorizationServices = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        var defaultAuthenticationScheme = configuration.GetValue<string>($"{sectionName}:DefaultScheme");

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultAuthenticationScheme;
            options.DefaultChallengeScheme = defaultAuthenticationScheme;
        });

        return builder.AddSimpleAuthentication(configuration, sectionName, addAuthorizationServices);
    }

    /// <summary>
    /// Registers services required by authentication services, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/> to add services to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="addAuthorizationServices">Set to <see langword="true"/> to automatically add Authorization policy services.</param>
    /// <returns>An <see cref="AuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentException">Configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="AuthenticationBuilder"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="AuthenticationBuilder"/>
    public static AuthenticationBuilder AddSimpleAuthentication(this AuthenticationBuilder builder, IConfiguration configuration, string sectionName = "Authentication", bool addAuthorizationServices = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        if (addAuthorizationServices)
        {
            builder.Services.AddAuthorization();
        }

        CheckAddJwtBearer(builder, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(builder, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(builder, configuration.GetSection($"{sectionName}:Basic"));

        return builder;

        static void CheckAddJwtBearer(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SchemeName, nameof(JwtBearerSettings.SchemeName));
            ArgumentNullException.ThrowIfNull(settings.SecurityKey, nameof(JwtBearerSettings.SecurityKey));
            ArgumentNullException.ThrowIfNull(settings.Algorithm, nameof(JwtBearerSettings.Algorithm));
            ArgumentNullException.ThrowIfNull(settings.NameClaimType, nameof(JwtBearerSettings.NameClaimType));
            ArgumentNullException.ThrowIfNull(settings.RoleClaimType, nameof(JwtBearerSettings.RoleClaimType));

            builder.Services.Configure<JwtBearerSettings>(section);

            builder.AddJwtBearer(settings.SchemeName, options =>
            {
                options.TokenValidationParameters = new()
                {
                    AuthenticationType = settings.SchemeName,
                    NameClaimType = settings.NameClaimType,
                    RoleClaimType = settings.RoleClaimType,
                    ValidateIssuer = settings.Issuers?.Any() ?? false,
                    ValidIssuers = settings.Issuers,
                    ValidateAudience = settings.Audiences?.Any() ?? false,
                    ValidAudiences = settings.Audiences,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey)),
                    RequireExpirationTime = true,
                    ValidateLifetime = settings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero,
                    ClockSkew = settings.ClockSkew
                };
            });

            if (settings.EnableJwtBearerService)
            {
                builder.Services.TryAddSingleton<IJwtBearerService, JwtBearerService>();
            }
        }

        static void CheckAddApiKey(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<ApiKeySettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SchemeName, nameof(ApiKeySettings.SchemeName));
            ArgumentNullException.ThrowIfNull(settings.NameClaimType, nameof(JwtBearerSettings.NameClaimType));
            ArgumentNullException.ThrowIfNull(settings.RoleClaimType, nameof(JwtBearerSettings.RoleClaimType));

            if (string.IsNullOrWhiteSpace(settings.HeaderName) && string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                throw new ArgumentException("You need to specify either a header name or a query string parameter key");
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKeyValue))
            {
                ArgumentNullException.ThrowIfNull(settings.UserName, nameof(ApiKeySettings.UserName));
            }

            if (settings.ApiKeys.Any(k => string.IsNullOrWhiteSpace(k.Value) || string.IsNullOrWhiteSpace(k.UserName)))
            {
                throw new ArgumentNullException("Api Keys", "One or more API Keys contain null values");
            }

            builder.Services.Configure<ApiKeySettings>(section);

            builder.AddScheme<ApiKeySettings, ApiKeyAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.HeaderName = settings.HeaderName;
                options.QueryStringKey = settings.QueryStringKey;
                options.ApiKeyValue = settings.ApiKeyValue;
                options.UserName = settings.UserName;
                options.ApiKeys = settings.ApiKeys;
                options.NameClaimType = settings.NameClaimType;
                options.RoleClaimType = settings.RoleClaimType;
            });
        }

        static void CheckAddBasicAuthentication(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SchemeName, nameof(BasicAuthenticationSettings.SchemeName));

            if (!string.IsNullOrWhiteSpace(settings.UserName))
            {
                ArgumentNullException.ThrowIfNull(settings.Password, nameof(BasicAuthenticationSettings.Password));
            }

            if (!string.IsNullOrWhiteSpace(settings.Password))
            {
                ArgumentNullException.ThrowIfNull(settings.UserName, nameof(BasicAuthenticationSettings.UserName));
            }

            if (settings.Credentials.Any(c => string.IsNullOrWhiteSpace(c.UserName) || string.IsNullOrWhiteSpace(c.Password)))
            {
                throw new ArgumentNullException("Credentials", "One or more credentials contain null values");
            }

            builder.Services.Configure<BasicAuthenticationSettings>(section);

            builder.AddScheme<BasicAuthenticationSettings, BasicAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.UserName = settings.UserName;
                options.Password = settings.Password;
                options.Credentials = settings.Credentials;
            });
        }
    }

    /// <summary>
    /// Adds the <see cref="AuthenticationMiddleware"/> and <see cref="AuthorizationMiddleware"/> middlewares to the
    /// specified <see cref="IApplicationBuilder"/>, which enable authentication and authorization capabilities.
    /// When authorizing a resource that is routed using endpoint routing, this call
    /// must appear between the calls to <c>app.UseRouting()</c> and <c>app.UseEndpoints(...)</c> for
    /// the middleware to function correctly.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add middlewares to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="AuthenticationMiddleware"/>
    /// <seealso cref="AuthorizationMiddleware"/>
    /// <seealso cref="IApplicationBuilder"/>
    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
