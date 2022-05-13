using System.Security.Claims;

namespace SimpleAuthentication;

public interface IJwtBearerGeneratorService
{
    string CreateToken(string username, IList<Claim>? claims = null, string? issuer = null, string? audience = null);
}