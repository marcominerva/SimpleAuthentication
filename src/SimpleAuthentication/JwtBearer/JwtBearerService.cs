using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthentication.Extensions;

namespace SimpleAuthentication.JwtBearer;

internal class JwtBearerService : IJwtBearerService
{
    private readonly JwtBearerSettings jwtBearerSettings;

    public JwtBearerService(IOptions<JwtBearerSettings> jwtBearerSettingsOptions)
    {
        jwtBearerSettings = jwtBearerSettingsOptions.Value;
    }

    public string CreateToken(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null)
    {
        claims ??= new List<Claim>();
        claims.Update(ClaimTypes.Name, userName);
        claims.Remove(JwtRegisteredClaimNames.Aud);

        var now = DateTime.UtcNow;

        var jwtSecurityToken = new JwtSecurityToken(
            issuer ?? jwtBearerSettings.Issuers?.FirstOrDefault(),
            audience ?? jwtBearerSettings.Audiences?.FirstOrDefault(),
            claims,
            now,
            absoluteExpiration ?? (jwtBearerSettings.ExpirationTime.GetValueOrDefault() > TimeSpan.Zero ? now.Add(jwtBearerSettings.ExpirationTime!.Value) : DateTime.MaxValue),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtBearerSettings.SecurityKey)), jwtBearerSettings.Algorithm));

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.WriteToken(jwtSecurityToken);

        return token;
    }

    public ClaimsPrincipal ValidateToken(string token, bool validateLifetime)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
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

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || jwtSecurityToken.Header.Alg != jwtBearerSettings.Algorithm)
        {
            throw new SecurityTokenException("Token is not a JWT or uses an unexpected algorithm.");
        }

        return principal;
    }

    public string RefreshToken(string token, bool validateLifetime, DateTime? absoluteExpiration = null)
    {
        var principal = ValidateToken(token, validateLifetime);
        var claims = (principal.Identity as ClaimsIdentity)!.Claims.ToList();

        var userName = claims.First(c => c.Type == ClaimTypes.Name).Value;
        var issuer = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Iss)?.Value;
        var audience = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;

        var newToken = CreateToken(userName, claims, issuer, audience, absoluteExpiration);
        return newToken;
    }
}
