using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAuthentication.JwtBearer;

/// <summary>
/// Options class provides information needed to control JWT Bearer Authentication handler behavior.
/// </summary>
public class JwtBearerSettings
{
    /// <summary>
    /// Gets or sets The authentication scheme name (Default: Bearer).
    /// </summary>
    public string SchemeName { get; set; } = JwtBearerDefaults.AuthenticationScheme;

    /// <summary>
    /// Gets or sets the cryptographic algorithm that is used to generate the digital signature (Default: HS256).
    /// </summary>
    /// <seealso cref="Microsoft.IdentityModel.Tokens.SecurityAlgorithms"/>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// Gets or sets the security key that is used to sign the token.
    /// </summary>
    public string SecurityKey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list that contains valid issuers that will be used to check against the token's issuer.
    /// </summary>
    /// <seealso cref="TokenValidationParameters.ValidIssuers"/>
    public string[]? Issuers { get; set; }

    /// <summary>
    /// Gets or sets the list that contains valid audiences that will be used to check against the token's audience.
    /// </summary>
    /// <seealso cref="TokenValidationParameters.ValidAudience"/>
    public string[]? Audiences { get; set; }

    /// <summary>
    /// Gets or sets the expiration time (relative to UTC current time) of the bearer token.
    /// </summary>
    public TimeSpan? ExpirationTime { get; set; }

    /// <summary>
    /// Gets or sets the clock skew to apply when validating a time (Default: 5 minutes).
    /// </summary>
    /// <seealso cref="TokenValidationParameters.ClockSkew"/>
    public TimeSpan ClockSkew { get; set; } = TokenValidationParameters.DefaultClockSkew;

    /// <summary>
    /// <see langword="true"/> to register the <see cref="IJwtBearerService"/> service in the <see cref="IServiceCollection"/> (Default: true).
    /// </summary>
    /// <seealso cref="IJwtBearerService"/>
    public bool EnableJwtBearerService { get; set; } = true;
}
