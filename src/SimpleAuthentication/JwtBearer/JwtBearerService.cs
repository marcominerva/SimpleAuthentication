using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAuthentication.JwtBearer;

internal class JwtBearerService(IOptions<JwtBearerSettings> jwtBearerSettingsOptions) : IJwtBearerService
{
    private readonly JwtBearerSettings jwtBearerSettings = jwtBearerSettingsOptions.Value;

    public Task<string> CreateTokenAsync(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null)
    {
        claims ??= [];
        claims.Update(jwtBearerSettings.NameClaimType, userName);
        claims.Update(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());

        var now = DateTime.UtcNow;

        var securityTokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer ?? jwtBearerSettings.Issuers?.FirstOrDefault(),
            Audience = audience ?? jwtBearerSettings.Audiences?.FirstOrDefault(),
            IssuedAt = now,
            NotBefore = now.Add(-jwtBearerSettings.ClockSkew),
            Expires = absoluteExpiration ?? (jwtBearerSettings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero ? now.Add(jwtBearerSettings.ExpirationTime!.Value) : DateTime.MaxValue),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBearerSettings.SecurityKey)), jwtBearerSettings.Algorithm)
        };

        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(securityTokenDescriptor);

        return Task.FromResult(token);
    }

    public async Task<ClaimsPrincipal> ValidateTokenAsync(string token, bool validateLifetime = true)
    {
        var tokenHandler = new JsonWebTokenHandler();

        if (!tokenHandler.CanReadToken(token))
        {
            throw new SecurityTokenException("Token is not a well formed Json Web Token (JWT)");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            AuthenticationType = jwtBearerSettings.SchemeName,
            NameClaimType = jwtBearerSettings.NameClaimType,
            RoleClaimType = jwtBearerSettings.RoleClaimType,
            ValidateIssuer = jwtBearerSettings.Issuers?.Any() ?? false,
            ValidIssuers = jwtBearerSettings.Issuers,
            ValidateAudience = jwtBearerSettings.Audiences?.Any() ?? false,
            ValidAudiences = jwtBearerSettings.Audiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBearerSettings.SecurityKey)),
            RequireExpirationTime = true,
            ValidateLifetime = validateLifetime,
            ClockSkew = jwtBearerSettings.ClockSkew
        };

        var validationResult = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);

        if (!validationResult.IsValid || validationResult.SecurityToken is not JsonWebToken jsonWebToken || jsonWebToken.Alg != jwtBearerSettings.Algorithm)
        {
            throw new SecurityTokenException("Token is expired or invalid", validationResult.Exception);
        }

        var principal = new ClaimsPrincipal(validationResult.ClaimsIdentity);
        return principal;
    }

    public async Task<string> RefreshTokenAsync(string token, bool validateLifetime, DateTime? absoluteExpiration = null)
    {
        var principal = await ValidateTokenAsync(token, validateLifetime);
        var claims = (principal.Identity as ClaimsIdentity)!.Claims.ToList();

        var userName = claims.First(c => c.Type == jwtBearerSettings.NameClaimType).Value;
        var issuer = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value;
        var audience = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;

        var newToken = await CreateTokenAsync(userName, claims, issuer, audience, absoluteExpiration);
        return newToken;
    }
}
