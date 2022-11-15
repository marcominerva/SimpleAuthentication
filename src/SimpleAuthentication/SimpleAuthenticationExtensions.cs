using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.BasicAuthentication;
using SimpleAuthentication.JwtBearer;
using SimpleAuthentication.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding authentication support in ASP.NET Core.
/// </summary>
/// <seealso cref="ISimpleAuthenticationBuilder"/>
/// <seealso cref="AuthenticationBuilder"/>
public static class SimpleAuthenticationExtensions
{
    /// <summary>
    /// Registers services required by authentication services, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="addAuthorizationServices">Set to <see langword="true"/> to automatically add Authorization policy services.</param>
    /// <returns>A <see cref="ISimpleAuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentException">Configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="IServiceCollection"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="ISimpleAuthenticationBuilder"/>    
    public static ISimpleAuthenticationBuilder AddSimpleAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication", bool addAuthorizationServices = true)
    {
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
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <param name="addAuthorizationServices">Set to <see langword="true"/> to automatically add Authorization policy services.</param>
    /// <returns>A <see cref="ISimpleAuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentException">Configuration is invalid.</exception>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="AuthenticationBuilder"/>
    /// <seealso cref="IConfiguration"/>
    /// <seealso cref="ISimpleAuthenticationBuilder"/>
    public static ISimpleAuthenticationBuilder AddSimpleAuthentication(this AuthenticationBuilder builder, IConfiguration configuration, string sectionName = "Authentication", bool addAuthorizationServices = true)
    {
        if (addAuthorizationServices)
        {
            builder.Services.AddAuthorization();
        }

        CheckAddJwtBearer(builder, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(builder, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(builder, configuration.GetSection($"{sectionName}:Basic"));

        return new DefaultSimpleAuthenticationBuilder(configuration, builder);

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

            builder.Services.Configure<JwtBearerSettings>(section);

            builder.AddJwtBearer(settings.SchemeName, options =>
            {
                options.TokenValidationParameters = new()
                {
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

            if (string.IsNullOrWhiteSpace(settings.HeaderName) && string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                throw new ArgumentException("You need to specify either a header name or a query string parameter key");
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKeyValue))
            {
                ArgumentNullException.ThrowIfNull(settings.DefaultUserName, nameof(ApiKeySettings.DefaultUserName));
            }

            builder.Services.Configure<ApiKeySettings>(section);

            builder.AddScheme<ApiKeySettings, ApiKeyAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.HeaderName = settings.HeaderName;
                options.QueryStringKey = settings.QueryStringKey;
                options.ApiKeyValue = settings.ApiKeyValue;
                options.DefaultUserName = settings.DefaultUserName;
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

            builder.Services.Configure<BasicAuthenticationSettings>(options =>
            {
                options.SchemeName = settings.SchemeName;
                options.UserName = settings.UserName;
                options.Password = settings.Password;
                options.IsEnabled = true;
            });

            builder.AddScheme<BasicAuthenticationSettings, BasicAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.UserName = settings.UserName;
                options.Password = settings.Password;
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
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>A reference to <paramref name="app"/> after the operation has completed.</returns>
    /// <seealso cref="AuthenticationMiddleware"/>
    /// <seealso cref="AuthorizationMiddleware"/>
    /// <seealso cref="IApplicationBuilder"/>
    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Adds authentication support in Swagger, enabling the Authorize button in the Swagger UI, reading configuration from the specified <see cref="IConfiguration"/> source.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to add configuration to.</param>
    /// <param name="configuration">The <see cref="IConfiguration"/> being bound.</param>
    /// <param name="sectionName">The name of the configuration section that holds authentication settings (default: Authentication).</param>
    /// <seealso cref="SwaggerGenOptions"/>
    /// <seealso cref="IConfiguration"/>
    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, string sectionName = "Authentication")
    {
        // Adds a security definition for each authentication method that has been configured.
        CheckAddJwtBearer(options, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(options, configuration.GetSection($"{sectionName}:ApiKey"));
        CheckAddBasicAuthentication(options, configuration.GetSection($"{sectionName}:Basic"));

        // This filters automatically adds a security requirement to each endpoint that requires authentication.
        options.OperationFilter<AuthenticationOperationFilter>();

        options.DocumentFilter<ProblemDetailsDocumentFilter>();

        static void CheckAddJwtBearer(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddAuthentication(options, settings.SchemeName, SecuritySchemeType.Http, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token");
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
                AddAuthentication(options, $"{settings.SchemeName} in Header", SecuritySchemeType.ApiKey, null, ParameterLocation.Header, settings.HeaderName, "Insert the API Key");
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryStringKey))
            {
                AddAuthentication(options, $"{settings.SchemeName} in Query String", SecuritySchemeType.ApiKey, null, ParameterLocation.Query, settings.QueryStringKey, "Insert the API Key");
            }
        }

        static void CheckAddBasicAuthentication(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<BasicAuthenticationSettings>();
            if (settings is null)
            {
                return;
            }

            AddAuthentication(options, settings.SchemeName, SecuritySchemeType.Http, BasicAuthenticationDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert user name and password");
        }

        static void AddAuthentication(SwaggerGenOptions options, string name, SecuritySchemeType securitySchemeType, string? scheme, ParameterLocation location, string parameterName, string description)
            => options.AddSecurityDefinition(name, new OpenApiSecurityScheme
            {
                In = location,
                Name = parameterName,
                Description = description,
                Type = securitySchemeType,
                Scheme = scheme
            });
    }
}
