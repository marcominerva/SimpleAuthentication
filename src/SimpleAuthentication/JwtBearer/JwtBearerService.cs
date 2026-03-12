using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAuthentication.JwtBearer;

/// <summary>
/// Default implementation of <see cref="IJwtBearerService"/> that provides JWT Bearer token generation and validation.
/// </summary>
/// <param name="jwtBearerSettingsOptions">The JWT Bearer settings.</param>
public class JwtBearerService(IOptions<JwtBearerSettings> jwtBearerSettingsOptions) : IJwtBearerService
{
    /// <summary>
    /// Gets the JWT Bearer settings used by this service.
    /// </summary>
    protected JwtBearerSettings JwtBearerSettings { get; } = jwtBearerSettingsOptions.Value;

    /// <inheritdoc />
    public virtual Task<string> CreateTokenAsync(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null)
    {
        claims ??= [];
        claims.Update(JwtBearerSettings.NameClaimType, userName);
        claims.Update(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());

        var now = DateTime.UtcNow;

        var securityTokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims, JwtBearerSettings.SchemeName, JwtBearerSettings.NameClaimType, JwtBearerSettings.RoleClaimType),
            Issuer = issuer ?? JwtBearerSettings.Issuers?.FirstOrDefault(),
            Audience = audience ?? JwtBearerSettings.Audiences?.FirstOrDefault(),
            IssuedAt = now,
            NotBefore = now.Add(-JwtBearerSettings.ClockSkew),
            Expires = absoluteExpiration ?? (JwtBearerSettings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero ? now.Add(JwtBearerSettings.ExpirationTime!.Value) : DateTime.MaxValue),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtBearerSettings.SecurityKey)), JwtBearerSettings.Algorithm)
        };

        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(securityTokenDescriptor);

        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public virtual async Task<ClaimsPrincipal> ValidateTokenAsync(string token, bool validateLifetime = true)
    {
        var tokenHandler = new JsonWebTokenHandler();

        if (!tokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenException("Token is not a well formed Json Web Token (JWT)");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            AuthenticationType = JwtBearerSettings.SchemeName,
            NameClaimType = JwtBearerSettings.NameClaimType,
            RoleClaimType = JwtBearerSettings.RoleClaimType,
            ValidateIssuer = JwtBearerSettings.Issuers?.Any() ?? false,
            ValidIssuers = JwtBearerSettings.Issuers,
            ValidateAudience = JwtBearerSettings.Audiences?.Any() ?? false,
            ValidAudiences = JwtBearerSettings.Audiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtBearerSettings.SecurityKey)),
            RequireExpirationTime = true,
            ValidateLifetime = validateLifetime,
            ClockSkew = JwtBearerSettings.ClockSkew
        };

        var validationResult = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);

        if (!validationResult.IsValid || validationResult.SecurityToken is not JsonWebToken jsonWebToken || jsonWebToken.Alg != JwtBearerSettings.Algorithm)
        {
            throw new SecurityTokenException("Token is expired or invalid", validationResult.Exception);
        }

        var principal = new ClaimsPrincipal(validationResult.ClaimsIdentity);
        return principal;
    }

    /// <inheritdoc />
    public virtual async Task<string> RefreshTokenAsync(string token, bool validateLifetime, DateTime? absoluteExpiration = null)
    {
        var principal = await ValidateTokenAsync(token, validateLifetime);
        var claims = (principal.Identity as ClaimsIdentity)!.Claims.ToList();

        var userName = claims.First(c => c.Type == JwtBearerSettings.NameClaimType).Value;
        var issuer = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value;
        var audience = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;

        var newToken = await CreateTokenAsync(userName, claims, issuer, audience, absoluteExpiration);
        return newToken;
    }
}
