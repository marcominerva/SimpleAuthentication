using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.JwtBearer;
using SimpleAuthentication.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthentication;

public static class SimpleAuthenticationExtensions
{
    public static ISimpleAuthenticationBuilder AddSimpleAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication")
    {
        var defaultAuthenticationScheme = configuration.GetValue<string>($"{sectionName}:DefaultAuthenticationScheme");

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultAuthenticationScheme;
            options.DefaultChallengeScheme = defaultAuthenticationScheme;
        });

        CheckAddJwtBearer(builder, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(builder, configuration.GetSection($"{sectionName}:ApiKey"));

        return new DefaultSimpleAuthenticationBuilder(configuration, builder);

        static void CheckAddJwtBearer(AuthenticationBuilder builder, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(settings.SecurityKey, nameof(JwtBearerSettings.SecurityKey));
            ArgumentNullException.ThrowIfNull(settings.Algorithm, nameof(JwtBearerSettings.Algorithm));

            builder.Services.Configure<JwtBearerSettings>(section);

            builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = settings.Issuers?.Any() ?? false,
                    ValidIssuers = settings.Issuers,
                    ValidateAudience = settings.Audiences?.Any() ?? false,
                    ValidAudiences = settings.Audiences,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey)),
                    ValidateLifetime = settings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero,
                    RequireExpirationTime = settings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero,
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

            if (string.IsNullOrWhiteSpace(settings.HeaderName) && string.IsNullOrWhiteSpace(settings.QueryName))
            {
                throw new ArgumentException("You need to specify either a header name or a query string parameter name");
            }

            if (!string.IsNullOrWhiteSpace(settings.ApiKeyValue))
            {
                ArgumentNullException.ThrowIfNull(settings.DefaultUsername, nameof(ApiKeySettings.DefaultUsername));
            }

            builder.Services.Configure<ApiKeySettings>(section);

            builder.AddScheme<ApiKeySettings, ApiKeyAuthenticationHandler>(settings.SchemeName, options =>
            {
                options.SchemeName = settings.SchemeName;
                options.HeaderName = settings.HeaderName;
                options.QueryName = settings.QueryName;
                options.ApiKeyValue = settings.ApiKeyValue;
                options.DefaultUsername = settings.DefaultUsername;
            });
        }
    }

    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static void AddSimpleAuthentication(this SwaggerGenOptions options, IConfiguration configuration, string sectionName = "Authentication")
    {
        CheckAddJwtBearer(options, configuration.GetSection($"{sectionName}:JwtBearer"));
        CheckAddApiKey(options, configuration.GetSection($"{sectionName}:ApiKey"));

        options.OperationFilter<AuthenticationOperationFilter>();
        options.DocumentFilter<ProblemDetailsDocumentFilter>();

        static void CheckAddJwtBearer(SwaggerGenOptions options, IConfigurationSection section)
        {
            var settings = section.Get<JwtBearerSettings>();
            if (settings is null)
            {
                return;
            }

            AddApiKeyAuthentication(options, JwtBearerDefaults.AuthenticationScheme, ParameterLocation.Header, HeaderNames.Authorization, "Insert the Bearer Token with the 'Bearer ' prefix");
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
                AddApiKeyAuthentication(options, $"{settings.SchemeName} in Header", ParameterLocation.Header, settings.HeaderName, "Insert the API Key");
            }

            if (!string.IsNullOrWhiteSpace(settings.QueryName))
            {
                AddApiKeyAuthentication(options, $"{settings.SchemeName} in Query String", ParameterLocation.Query, settings.QueryName, "Insert the API Key");
            }
        }

        static void AddApiKeyAuthentication(SwaggerGenOptions options, string schemeName, ParameterLocation location, string name, string description)
            => options.AddSecurityDefinition(schemeName, new OpenApiSecurityScheme
            {
                In = location,
                Name = name,
                Description = description,
                Type = SecuritySchemeType.ApiKey
            });
    }
}
