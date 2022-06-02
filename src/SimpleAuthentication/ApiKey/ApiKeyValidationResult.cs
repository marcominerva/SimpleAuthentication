using System.Security.Claims;

namespace SimpleAuthentication.ApiKey;

/// <summary>
/// Contains the result of an API key authentication flow.
/// </summary>
public class ApiKeyValidationResult
{
    /// <summary>
    /// <see langword="true"/> if authentication was successful; <see langword="false"/> otherwise.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the authenticated username, if authentication was successful.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets the claim list, if authentication was successful.
    /// </summary>
    public IList<Claim>? Claims { get; }

    /// <summary>
    /// Gets the failure message, if authentication was unsuccessful.
    /// </summary>
    public string? FailureMessage { get; }

    private ApiKeyValidationResult(string username, IList<Claim>? claims)
    {
        Succeeded = true;
        Username = username;
        Claims = claims;
    }

    private ApiKeyValidationResult(string failureMessage)
    {
        FailureMessage = failureMessage;
    }

    /// <summary>
    /// Indicates that authentication was successful.
    /// </summary>
    /// <param name="username">The username to be used.</param>
    /// <param name="claims">The claims list.</param>
    /// <returns>The authentication result.</returns>
    /// <seealso cref="IApiKeyValidator"/>
    public static ApiKeyValidationResult Success(string username, IList<Claim>? claims = null)
        => new(username, claims);

    /// <summary>
    /// Indicates that there was a failure during authentication.
    /// </summary>
    /// <param name="failureMessage">The failure message.</param>
    /// <returns>The authentication result.</returns>
    /// <seealso cref="IApiKeyValidator"/>
    public static ApiKeyValidationResult Fail(string failureMessage)
        => new(failureMessage);
}

