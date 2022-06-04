using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthentication.JwtBearer;

/// <summary>
/// Provides methods for JWT Bearer generation and validation.
/// </summary>
public interface IJwtBearerService
{
    /// <summary>
    /// Creates a bearer token using the setting specified in the <see cref="IConfiguration"/> source, with the ability to override some parameters.
    /// </summary>
    /// <param name="userName">The user name that must be stored in the token.</param>
    /// <param name="claims">The claims list.</param>
    /// <param name="issuer">The issuer of the bearer. If <see langword="null"/>, the first issuer specified in the configuration will be used, if any.</param>
    /// <param name="audience">The audience of the bearer. If <see langword="null"/>, the first audience specified in the configuration will be used, if any.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <returns></returns>
    string CreateToken(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null);

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    ClaimsPrincipal ValidateToken(string token)
        => ValidateToken(token, true);

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    ClaimsPrincipal ValidateToken(string token, bool validateLifetime);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</param>
    /// <returns><see langword="true"/> is the validation was successful; otherwise, <see langword="false"/>.</returns>
    bool TryValidateToken(string token, out ClaimsPrincipal? claimsPrincipal)
        => TryValidateToken(token, true, out claimsPrincipal);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</param>
    /// <returns><see langword="true"/> is the validation was successful; otherwise, <see langword="false"/>.</returns>
    bool TryValidateToken(string token, bool validateLifetime, out ClaimsPrincipal? claimsPrincipal)
    {
        try
        {
            var principal = ValidateToken(token, validateLifetime);
            claimsPrincipal = principal;
            return true;
        }
        catch
        {
            claimsPrincipal = null;
            return false;
        }
    }
}