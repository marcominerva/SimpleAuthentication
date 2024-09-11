using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

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
    /// <returns>The JWT bearer token.</returns>
    /// <exception cref="ArgumentException"><paramref name="absoluteExpiration"/> is &lt; DateTime.UtcNow.</exception>    
    [Obsolete("This method has been deprecated and will be removed in a future version. Use CreateTokenAsync instead.")]
    string CreateToken(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null)
        => CreateTokenAsync(userName, claims, issuer, audience, absoluteExpiration).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Creates a bearer token using the setting specified in the <see cref="IConfiguration"/> source, with the ability to override some parameters.
    /// </summary>
    /// <param name="userName">The user name that must be stored in the token.</param>
    /// <param name="claims">The claims list.</param>
    /// <param name="issuer">The issuer of the bearer. If <see langword="null"/>, the first issuer specified in the configuration will be used, if any.</param>
    /// <param name="audience">The audience of the bearer. If <see langword="null"/>, the first audience specified in the configuration will be used, if any.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>    /// <returns>The JWT bearer token.</returns>
    /// <exception cref="ArgumentException"><paramref name="absoluteExpiration"/> is &lt; DateTime.UtcNow.</exception>    
    Task<string> CreateTokenAsync(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    [Obsolete("This method has been deprecated and will be removed in a future version. Use ValidateTokenAsync instead.")]
    ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true)
        => ValidateTokenAsync(token, validateLifetime).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    Task<ClaimsPrincipal> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        => ValidateTokenAsync(token, true, cancellationToken);

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    Task<ClaimsPrincipal> ValidateTokenAsync(string token, bool validateLifetime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</param>
    /// <returns><see langword="true"/> is the validation was successful; otherwise, <see langword="false"/>.</returns>
    [Obsolete("This method has been deprecated and will be removed in a future version. Use TryValidateTokenAsync instead.")]
    bool TryValidateToken(string token, [NotNullWhen(true)] out ClaimsPrincipal? claimsPrincipal)
        => TryValidateToken(token, true, out claimsPrincipal);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</param>
    /// <returns><see langword="true"/> is the validation was successful; otherwise, <see langword="false"/>.</returns>
    [Obsolete("This method has been deprecated and will be removed in a future version. Use TryValidateTokenAsync instead.")]
    bool TryValidateToken(string token, bool validateLifetime, [NotNullWhen(true)] out ClaimsPrincipal? claimsPrincipal)
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

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JwtBearerValidationResult"/> that contains the result of the validation.</returns>
    /// <see cref="JwtBearerValidationResult"/>
    Task<JwtBearerValidationResult> TryValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        => TryValidateTokenAsync(token, true, cancellationToken);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JwtBearerValidationResult"/> that contains the result of the validation.</returns>
    /// <see cref="JwtBearerValidationResult"/>
    async Task<JwtBearerValidationResult> TryValidateTokenAsync(string token, bool validateLifetime, CancellationToken cancellationToken = default)
    {
        var result = new JwtBearerValidationResult();

        try
        {
            var principal = await ValidateTokenAsync(token, validateLifetime, cancellationToken);
            result = new JwtBearerValidationResult { IsValid = true, Principal = principal };
        }
        catch (Exception ex)
        {
            result = new JwtBearerValidationResult { IsValid = false, Exception = ex };
        }

        return result;
    }

    /// <summary>
    /// Refresh a valid token, extending its expiration.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <returns>The JWT bearer containing all the information of the input <paramref name="token"/>, with an extended expiration.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    [Obsolete("This method has been deprecated and will be removed in a future version. Use RefreshTokenAsync instead.")]
    string RefreshToken(string token, DateTime? absoluteExpiration = null)
        => RefreshTokenAsync(token, true, absoluteExpiration).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Refresh a valid token, extending its expiration.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <returns>The JWT bearer containing all the information of the input <paramref name="token"/>, with an extended expiration.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    [Obsolete("This method has been deprecated and will be removed in a future version. Use RefreshTokenAsync instead.")]
    string RefreshToken(string token, bool validateLifetime, DateTime? absoluteExpiration = null)
        => RefreshTokenAsync(token, validateLifetime, absoluteExpiration).ConfigureAwait(false).GetAwaiter().GetResult();

    /// <summary>
    /// Refresh a valid token, extending its expiration.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The JWT bearer containing all the information of the input <paramref name="token"/>, with an extended expiration.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    Task<string> RefreshTokenAsync(string token, DateTime? absoluteExpiration = null, CancellationToken cancellationToken = default)
        => RefreshTokenAsync(token, true, absoluteExpiration, cancellationToken);

    /// <summary>
    /// Refresh a valid token, extending its expiration.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The JWT bearer containing all the information of the input <paramref name="token"/>, with an extended expiration.</returns>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is expired or invalid.</exception>
    Task<string> RefreshTokenAsync(string token, bool validateLifetime, DateTime? absoluteExpiration = null, CancellationToken cancellationToken = default);
}