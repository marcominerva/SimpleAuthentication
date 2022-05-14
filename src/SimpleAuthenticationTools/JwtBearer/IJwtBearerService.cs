using System.Security.Claims;

namespace SimpleAuthenticationTools.JwtBearer;

public interface IJwtBearerService
{
    string CreateToken(string username, IList<Claim>? claims = null, string? issuer = null, string? audience = null);
}