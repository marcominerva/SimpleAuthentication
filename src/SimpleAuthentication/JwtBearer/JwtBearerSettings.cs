using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SimpleAuthentication.JwtBearer;

/// <summary>
/// Options class provides information needed to control Bearer Authentication handler behavior
/// </summary>
public class JwtBearerSettings
{
    /// <summary>
    /// Gets or sets The authentication scheme name (Default: Bearer).
    /// </summary>
    public string SchemeName { get; set; } = JwtBearerDefaults.AuthenticationScheme;

    /// <summary>
    /// Gets or sets the cryptographic algorithm that is used to generate the digital signature. (Default: HS256).
    /// </summary>
    /// <seealso cref="Microsoft.IdentityModel.Tokens.SecurityAlgorithms"/>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// Gets or sets the security key that is used to sign the token.
    /// </summary>
    public string SecurityKey { get; set; } = null!;

    public string[]? Issuers { get; set; }

    public string[]? Audiences { get; set; }

    public TimeSpan? ExpirationTime { get; set; }

    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    public bool EnableJwtBearerService { get; set; } = true;
}
