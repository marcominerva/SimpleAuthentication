using System.Security.Claims;

namespace SimpleAuthentication;

public interface IJwtTokenGeneratorService
{
    string CreateToken(string username, IList<Claim>? claims = null, string? issuer = null, string? audience = null);
}