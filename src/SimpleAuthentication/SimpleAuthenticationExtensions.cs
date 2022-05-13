using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAuthentication;

public static class SimpleAuthenticationExtensions
{
    public static ISimpleAuthenticationBuilder AddSimpleAuthentication(this IServiceCollection services)
        => new DefaultSimpleAuthenticationBuilder(services);

    public static IJwtAuthenticationBuilder WithJwtBearer(this ISimpleAuthenticationBuilder builder, IConfiguration configuration, string sectionName = "JwtBearer")
    {
        var section = configuration.GetSection(sectionName);
        var settings = section.Get<JwtBearerSettings>();
        builder.Services.Configure<JwtBearerSettings>(section);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = settings.Issuers?.Any() ?? false,
                ValidIssuers = settings.Issuers,
                ValidateAudience = settings.Audiences?.Any() ?? false,
                ValidAudiences = settings.Audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(settings.SecurityKey),
                IssuerSigningKey = !string.IsNullOrWhiteSpace(settings.SecurityKey)
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey))
                    : null,
                RequireExpirationTime = settings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero,
                ClockSkew = TimeSpan.Zero
            };
        });

        return new DefaultJwtAuthenticationBuilder(builder.Services);
    }

    public static IJwtAuthenticationBuilder AddJwtBearerGenerator(this IJwtAuthenticationBuilder builder)
    {
        builder.Services.TryAddSingleton<IJwtBearerGeneratorService, JwtBearerGeneratorService>();
        return builder;
    }

    public static IApplicationBuilder UseSimpleAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
