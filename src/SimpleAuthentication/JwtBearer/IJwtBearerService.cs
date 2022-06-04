using System.IdentityModel.Tokens.Jwt;
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
    string CreateToken(string userName, IList<Claim>? claims = null, string? issuer = null, string? audience = null, DateTime? absoluteExpiration = null);

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/>.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> does not have 3 or 5 parts.</exception>
    /// <exception cref="ArgumentException"><see cref="JwtSecurityTokenHandler.CanReadToken(string)"/> returns false.</exception>
    /// <exception cref="SecurityTokenDecryptionFailedException"><paramref name="token"/> was a JWE was not able to be decrypted.</exception>
    /// <exception cref="SecurityTokenEncryptionKeyNotFoundException"><paramref name="token"/> 'kid' header claim is not null AND decryption fails.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> 'enc' header claim is null or empty.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is not a <see cref="JwtSecurityToken"/> or 'alg' header contains an unexpected value.</exception>
    /// <exception cref="SecurityTokenExpiredException"><paramref name="token"/> 'exp' claim is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenInvalidAudienceException"><paramref name="token"/> 'aud' claim did not match either <see cref="TokenValidationParameters.ValidAudience"/> or one of <see cref="TokenValidationParameters.ValidAudiences"/>.</exception>
    /// <exception cref="SecurityTokenInvalidLifetimeException"><paramref name="token"/> 'nbf' claim is &gt; 'exp' claim.</exception>
    /// <exception cref="SecurityTokenInvalidSignatureException"><paramref name="token"/>.signature is not properly formatted.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><paramref name="token"/> 'exp' claim is missing and <see cref="TokenValidationParameters.RequireExpirationTime"/> is true.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><see cref="TokenValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
    /// <exception cref="SecurityTokenNotYetValidException"><paramref name="token"/> 'nbf' claim is &gt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenReplayAddFailedException"><paramref name="token"/> could not be added to the <see cref="TokenValidationParameters.TokenReplayCache"/>.</exception>
    /// <exception cref="SecurityTokenReplayDetectedException"><paramref name="token"/> is found in the cache.</exception>
    ClaimsPrincipal ValidateToken(string token)
        => ValidateToken(token, true);

    /// <summary>
    /// Reads and validates a 'JSON Web Token' (JWT) encoded as a JWS or JWE in Compact Serialized Format.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <returns>A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/>.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> does not have 3 or 5 parts.</exception>
    /// <exception cref="ArgumentException"><see cref="JwtSecurityTokenHandler.CanReadToken(string)"/> returns false.</exception>
    /// <exception cref="SecurityTokenDecryptionFailedException"><paramref name="token"/> was a JWE was not able to be decrypted.</exception>
    /// <exception cref="SecurityTokenEncryptionKeyNotFoundException"><paramref name="token"/> 'kid' header claim is not null AND decryption fails.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> 'enc' header claim is null or empty.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is not a <see cref="JwtSecurityToken"/> or 'alg' header contains an unexpected value.</exception>
    /// <exception cref="SecurityTokenExpiredException"><paramref name="validateLifetime"/> is <see langword="true"/> and <paramref name="token"/> 'exp' claim is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenInvalidAudienceException"><paramref name="token"/> 'aud' claim did not match either <see cref="TokenValidationParameters.ValidAudience"/> or one of <see cref="TokenValidationParameters.ValidAudiences"/>.</exception>
    /// <exception cref="SecurityTokenInvalidLifetimeException"><paramref name="token"/> 'nbf' claim is &gt; 'exp' claim.</exception>
    /// <exception cref="SecurityTokenInvalidSignatureException"><paramref name="token"/>.signature is not properly formatted.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><paramref name="token"/> 'exp' claim is missing and <see cref="TokenValidationParameters.RequireExpirationTime"/> is true.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><see cref="TokenValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
    /// <exception cref="SecurityTokenNotYetValidException"><paramref name="token"/> 'nbf' claim is &gt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenReplayAddFailedException"><paramref name="token"/> could not be added to the <see cref="TokenValidationParameters.TokenReplayCache"/>.</exception>
    /// <exception cref="SecurityTokenReplayDetectedException"><paramref name="token"/> is found in the cache.</exception>
    ClaimsPrincipal ValidateToken(string token, bool validateLifetime);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</param>
    /// <returns><see langword="true"/> is the validation was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/>.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> does not have 3 or 5 parts.</exception>
    /// <exception cref="ArgumentException"><see cref="JwtSecurityTokenHandler.CanReadToken(string)"/> returns false.</exception>
    /// <exception cref="SecurityTokenDecryptionFailedException"><paramref name="token"/> was a JWE was not able to be decrypted.</exception>
    /// <exception cref="SecurityTokenEncryptionKeyNotFoundException"><paramref name="token"/> 'kid' header claim is not null AND decryption fails.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> 'enc' header claim is null or empty.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is not a <see cref="JwtSecurityToken"/> or 'alg' header contains an unexpected value.</exception>
    /// <exception cref="SecurityTokenExpiredException"><paramref name="token"/> 'exp' claim is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenInvalidAudienceException"><paramref name="token"/> 'aud' claim did not match either <see cref="TokenValidationParameters.ValidAudience"/> or one of <see cref="TokenValidationParameters.ValidAudiences"/>.</exception>
    /// <exception cref="SecurityTokenInvalidLifetimeException"><paramref name="token"/> 'nbf' claim is &gt; 'exp' claim.</exception>
    /// <exception cref="SecurityTokenInvalidSignatureException"><paramref name="token"/>.signature is not properly formatted.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><paramref name="token"/> 'exp' claim is missing and <see cref="TokenValidationParameters.RequireExpirationTime"/> is true.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><see cref="TokenValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
    /// <exception cref="SecurityTokenNotYetValidException"><paramref name="token"/> 'nbf' claim is &gt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenReplayAddFailedException"><paramref name="token"/> could not be added to the <see cref="TokenValidationParameters.TokenReplayCache"/>.</exception>
    /// <exception cref="SecurityTokenReplayDetectedException"><paramref name="token"/> is found in the cache.</exception>
    bool TryValidateToken(string token, out ClaimsPrincipal? claimsPrincipal)
        => TryValidateToken(token, true, out claimsPrincipal);

    /// <summary>
    /// Try to read and validate a bearer token.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="claimsPrincipal">A <see cref="ClaimsPrincipal"/> from the JWT. Does not include claims found in the JWT header.</param>
    /// <returns><see langword="true"/> is the validation was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/>.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> does not have 3 or 5 parts.</exception>
    /// <exception cref="ArgumentException"><see cref="JwtSecurityTokenHandler.CanReadToken(string)"/> returns false.</exception>
    /// <exception cref="SecurityTokenDecryptionFailedException"><paramref name="token"/> was a JWE was not able to be decrypted.</exception>
    /// <exception cref="SecurityTokenEncryptionKeyNotFoundException"><paramref name="token"/> 'kid' header claim is not null AND decryption fails.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> 'enc' header claim is null or empty.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is not a <see cref="JwtSecurityToken"/> or 'alg' header contains an unexpected value.</exception>
    /// <exception cref="SecurityTokenExpiredException"><paramref name="validateLifetime"/> is <see langword="true"/> and <paramref name="token"/> 'exp' claim is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenInvalidAudienceException"><paramref name="token"/> 'aud' claim did not match either <see cref="TokenValidationParameters.ValidAudience"/> or one of <see cref="TokenValidationParameters.ValidAudiences"/>.</exception>
    /// <exception cref="SecurityTokenInvalidLifetimeException"><paramref name="token"/> 'nbf' claim is &gt; 'exp' claim.</exception>
    /// <exception cref="SecurityTokenInvalidSignatureException"><paramref name="token"/>.signature is not properly formatted.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><paramref name="token"/> 'exp' claim is missing and <see cref="TokenValidationParameters.RequireExpirationTime"/> is true.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><see cref="TokenValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
    /// <exception cref="SecurityTokenNotYetValidException"><paramref name="token"/> 'nbf' claim is &gt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenReplayAddFailedException"><paramref name="token"/> could not be added to the <see cref="TokenValidationParameters.TokenReplayCache"/>.</exception>
    /// <exception cref="SecurityTokenReplayDetectedException"><paramref name="token"/> is found in the cache.</exception>
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

    /// <summary>
    /// Refresh a valid token, extending its expiration.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <returns>The JWT bearer token containing all the information of the input <paramref name="token"/>, with an extended expiration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/>.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> does not have 3 or 5 parts.</exception>
    /// <exception cref="ArgumentException"><see cref="JwtSecurityTokenHandler.CanReadToken(string)"/> returns false.</exception>
    /// <exception cref="SecurityTokenDecryptionFailedException"><paramref name="token"/> was a JWE was not able to be decrypted.</exception>
    /// <exception cref="SecurityTokenEncryptionKeyNotFoundException"><paramref name="token"/> 'kid' header claim is not null AND decryption fails.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> 'enc' header claim is null or empty.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is not a <see cref="JwtSecurityToken"/> or 'alg' header contains an unexpected value.</exception>
    /// <exception cref="SecurityTokenExpiredException"><paramref name="token"/> 'exp' claim is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenInvalidAudienceException"><paramref name="token"/> 'aud' claim did not match either <see cref="TokenValidationParameters.ValidAudience"/> or one of <see cref="TokenValidationParameters.ValidAudiences"/>.</exception>
    /// <exception cref="SecurityTokenInvalidLifetimeException"><paramref name="token"/> 'nbf' claim is &gt; 'exp' claim.</exception>
    /// <exception cref="SecurityTokenInvalidSignatureException"><paramref name="token"/>.signature is not properly formatted.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><paramref name="token"/> 'exp' claim is missing and <see cref="TokenValidationParameters.RequireExpirationTime"/> is true.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><see cref="TokenValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
    /// <exception cref="SecurityTokenNotYetValidException"><paramref name="token"/> 'nbf' claim is &gt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenReplayAddFailedException"><paramref name="token"/> could not be added to the <see cref="TokenValidationParameters.TokenReplayCache"/>.</exception>
    /// <exception cref="SecurityTokenReplayDetectedException"><paramref name="token"/> is found in the cache.</exception>
    string RefreshToken(string token, DateTime? absoluteExpiration = null)
        => RefreshToken(token, true, absoluteExpiration);

    /// <summary>
    /// Refresh a valid token, extending its expiration.
    /// </summary>
    /// <param name="token">The JWT encoded as JWE or JWS.</param>
    /// <param name="validateLifetime"><see langword="true"/> to validate the lifetime of the token.</param>
    /// <param name="absoluteExpiration">The absolute expiration of the token. If <see langword="null"/>, the expiration time specified in the configuration will be used, if any.</param>
    /// <returns>The JWT bearer token containing all the information of the input <paramref name="token"/>, with an extended expiration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/>.Length is greater than <see cref="TokenHandler.MaximumTokenSizeInBytes"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="token"/> does not have 3 or 5 parts.</exception>
    /// <exception cref="ArgumentException"><see cref="JwtSecurityTokenHandler.CanReadToken(string)"/> returns false.</exception>
    /// <exception cref="ArgumentException"><paramref name="absoluteExpiration"/> is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenDecryptionFailedException"><paramref name="token"/> was a JWE was not able to be decrypted.</exception>
    /// <exception cref="SecurityTokenEncryptionKeyNotFoundException"><paramref name="token"/> 'kid' header claim is not null AND decryption fails.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> 'enc' header claim is null or empty.</exception>
    /// <exception cref="SecurityTokenException"><paramref name="token"/> is not a <see cref="JwtSecurityToken"/> or 'alg' header contains an unexpected value.</exception>
    /// <exception cref="SecurityTokenExpiredException"><paramref name="validateLifetime"/> is <see langword="true"/> and <paramref name="token"/> 'exp' claim is &lt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenInvalidAudienceException"><paramref name="token"/> 'aud' claim did not match either <see cref="TokenValidationParameters.ValidAudience"/> or one of <see cref="TokenValidationParameters.ValidAudiences"/>.</exception>
    /// <exception cref="SecurityTokenInvalidLifetimeException"><paramref name="token"/> 'nbf' claim is &gt; 'exp' claim.</exception>
    /// <exception cref="SecurityTokenInvalidSignatureException"><paramref name="token"/>.signature is not properly formatted.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><paramref name="token"/> 'exp' claim is missing and <see cref="TokenValidationParameters.RequireExpirationTime"/> is true.</exception>
    /// <exception cref="SecurityTokenNoExpirationException"><see cref="TokenValidationParameters.TokenReplayCache"/> is not null and expirationTime.HasValue is false. When a TokenReplayCache is set, tokens require an expiration time.</exception>
    /// <exception cref="SecurityTokenNotYetValidException"><paramref name="token"/> 'nbf' claim is &gt; DateTime.UtcNow.</exception>
    /// <exception cref="SecurityTokenReplayAddFailedException"><paramref name="token"/> could not be added to the <see cref="TokenValidationParameters.TokenReplayCache"/>.</exception>
    /// <exception cref="SecurityTokenReplayDetectedException"><paramref name="token"/> is found in the cache.</exception>
    string RefreshToken(string token, bool validateLifetime, DateTime? absoluteExpiration = null);
}