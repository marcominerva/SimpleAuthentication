namespace SimpleAuthentication.ApiKey;

/// <summary>
/// Provides methods for API key validation.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Provides custom behavior for API key validation.
    /// </summary>
    /// <param name="apiKey">The API key to validate.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The <see cref="ApiKeyValidationResult"/> that contains the validation result.</returns>
    /// <seealso cref="ApiKeyValidationResult"/>
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);
}
