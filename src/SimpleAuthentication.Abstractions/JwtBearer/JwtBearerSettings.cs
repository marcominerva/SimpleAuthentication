using System.Security.Claims;
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
    /// <seealso cref="SecurityAlgorithms"/>
    public string Algorithm { get; set; } = SecurityAlgorithms.HmacSha256;

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
    /// Gets or sets a <see cref="string"/> that defines the <see cref="ClaimsIdentity.NameClaimType"/>.
    /// </summary>
    /// <remarks>
    /// Controls the value <see cref="ClaimsIdentity.Name"/> returns. It will return the first <see cref="Claim.Value"/> where the <see cref="Claim.Type"/> equals <see cref="NameClaimType"/>.
    /// The default is <see cref="ClaimsIdentity.DefaultNameClaimType"/>.
    /// </remarks>
    public string NameClaimType { get; set; } = ClaimsIdentity.DefaultNameClaimType;

    /// <summary>
    /// Gets or sets the <see cref="string"/> that defines the <see cref="ClaimsIdentity.RoleClaimType"/>.
    /// </summary>
    /// <remarks>
    /// <para>Controls the results of <see cref="ClaimsPrincipal.IsInRole( string )"/>.</para>
    /// <para>Each <see cref="Claim"/> where <see cref="Claim.Type"/> == <see cref="RoleClaimType"/> will be checked for a match against the 'string' passed to <see cref="ClaimsPrincipal.IsInRole(string)"/>.</para>
    /// The default is <see cref="ClaimsIdentity.DefaultRoleClaimType"/>.
    /// </remarks>
    public string RoleClaimType { get; set; } = ClaimsIdentity.DefaultRoleClaimType;

    /// <summary>
    /// <see langword="true"/> to register the <see cref="IJwtBearerService"/> service in the <see cref="IServiceCollection"/> (Default: true).
    /// </summary>
    /// <seealso cref="IJwtBearerService"/>
    public bool EnableJwtBearerService { get; set; } = true;
}
