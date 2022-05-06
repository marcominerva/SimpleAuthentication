using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAuthentication;

internal class JwtTokenGeneratorService : IJwtTokenGeneratorService
{
    private readonly JwtSettings jwtSettings;

    public JwtTokenGeneratorService(IOptions<JwtSettings> jwtSettingsOptions)
    {
        jwtSettings = jwtSettingsOptions.Value;
    }

    public string CreateToken(string username, IList<Claim>? claims = null, string? issuer = null, string? audience = null)
    {
        claims ??= new List<Claim>();
        if (!claims.Any(c => c.Type == ClaimTypes.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, username));
        }

        var signingCredentials = !string.IsNullOrWhiteSpace(jwtSettings.SecurityKey)
            ? new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey)), SecurityAlgorithms.HmacSha256)
            : null;

        var jwtSecurityToken = new JwtSecurityToken(
            issuer ?? jwtSettings.Issuers?.FirstOrDefault(),
            audience ?? jwtSettings.Audiences?.FirstOrDefault(),
            claims,
            DateTime.UtcNow,
            jwtSettings.AccessTokenExpirationMinutes > 0 ? DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpirationMinutes) : null,
            signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        return accessToken;
    }
}
