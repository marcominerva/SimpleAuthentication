using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthentication.JwtBearer;

/// <summary>
/// Provides methods for JWT Bearer generation and validation.
/// </summary>
public interface IJwtBearerService
{
    /// <summary>
    /// Creates a bearer using the setting specified in the <see cref="IConfiguration"/> source, with the ability to override some parameters.
    /// </summary>
    /// <param name="username">The username that must be stored in the token.</param>
    /// <param name="claims">The claims list.</param>
    /// <param name="issuer">The issuer of the bearer. If <see langword="null"/>, the first issuer specified in the configuration will be used, if any.</param>
    /// <param name="audience">The audience of the bearer. If <see langword="null"/>, the first audience specified in the configuration will be used, if any.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <returns></returns>
    string CreateToken(string username, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null);
}