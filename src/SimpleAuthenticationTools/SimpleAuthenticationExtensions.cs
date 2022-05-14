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
using SimpleAuthenticationTools.JwtBearer;
using SimpleAuthenticationTools.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SimpleAuthenticationTools;

public static class SimpleAuthenticationToolsExtensions
{
    public static ISimpleAuthenticationToolsBuilder AddSimpleAuthentication(this IServiceCollection services, IConfiguration configuration, string sectionName = "Authentication")
    {
        var defaultAuthenticationScheme = configuration.GetValue<string>($"{sectionName}:DefaultAuthenticationScheme");

        var builder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = defaultAuthenticationScheme;
            options.DefaultChallengeScheme = defaultAuthenticationScheme;
        });

        CheckAddJwtBearer(configuration.GetSection($"{sectionName}:JwtBearer"), builder);

        return new DefaultSimpleAuthenticationToolsBuilder(configuration, builder);
    }

    private static void CheckAddJwtBearer(IConfigurationSection section, AuthenticationBuilder builder)
    {
        var settings = section.Get<JwtBearerSettings>();
        if (settings is null)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(settings.SecurityKey, "SecurityKey");

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

    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static void AddJwtBearerAuthentication(this SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Insert the Bearer Token with the 'Bearer ' prefix",
            Name = HeaderNames.Authorization,
            Type = SecuritySchemeType.ApiKey
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference= new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                },
                Array.Empty<string>()
            }
        });

        options.OperationFilter<AuthenticationResponseOperationFilter>();
        options.DocumentFilter<ProblemDetailsDocumentFilter>();
    }
}
