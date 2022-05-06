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

    public static IJwtAuthenticationBuilder WithJwtBearer(this ISimpleAuthenticationBuilder builder, IConfiguration configuration, string sectionName = "JwtSettings")
    {
        var section = configuration.GetSection(sectionName);
        var jwtSettings = section.Get<JwtSettings>();
        builder.Services.Configure<JwtSettings>(section);

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = jwtSettings.Issuers?.Any() ?? false,
                ValidIssuers = jwtSettings.Issuers,
                ValidateAudience = jwtSettings.Audiences?.Any() ?? false,
                ValidAudiences = jwtSettings.Audiences,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = !string.IsNullOrWhiteSpace(jwtSettings.SecurityKey),
                IssuerSigningKey = !string.IsNullOrWhiteSpace(jwtSettings.SecurityKey)
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey))
                    : null,
                RequireExpirationTime = jwtSettings.AccessTokenExpirationMinutes > 0,
                ClockSkew = TimeSpan.Zero
            };
        });

        return new DefaultJwtAuthenticationBuilder(builder.Services);
    }

    public static IJwtAuthenticationBuilder AddJwtTokenGenerator(this IJwtAuthenticationBuilder builder)
    {
        builder.Services.TryAddSingleton<IJwtTokenGeneratorService, JwtTokenGeneratorService>();
        return builder;
    }

    public static IApplicationBuilder UseSimpleAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
