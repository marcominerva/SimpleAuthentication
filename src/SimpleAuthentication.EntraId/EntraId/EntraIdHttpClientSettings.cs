namespace SimpleAuthentication.EntraId;

/// <summary>
/// Options class provides information needed to call an API that is protected by Microsoft Entra ID using an app-only access token.
/// </summary>
public class EntraIdHttpClientSettings
{
    /// <summary>
    /// Gets or sets the base address of the API to call. It must be an absolute HTTPS URI that ends with a trailing slash.
    /// </summary>
    /// <remarks>
    /// The HTTP client can send requests only to addresses that start with this value, so that the access token cannot be sent to a different destination.
    /// </remarks>
    /// <seealso cref="AllowHttpOnLoopback"/>
    public Uri BaseAddress { get; set; } = null!;

    /// <summary>
    /// Gets or sets the scope that is used to request the access token, in the <c>{Application ID URI}/.default</c> format
    /// (for example <c>api://00000000-0000-0000-0000-000000000000/.default</c>).
    /// </summary>
    /// <remarks>
    /// App-only tokens are always requested for all the application roles that have been granted to the caller on a single API,
    /// so exactly one scope that ends with <c>/.default</c> is required.
    /// </remarks>
    public string Scope { get; set; } = null!;

    /// <summary>
    /// Gets or sets the name of the authentication scheme whose Microsoft Entra ID configuration (client ID and credentials) is used to acquire the access token (Default: EntraId).
    /// </summary>
    /// <seealso cref="EntraIdDefaults.AuthenticationScheme"/>
    public string AuthenticationScheme { get; set; } = EntraIdDefaults.AuthenticationScheme;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="BaseAddress"/> can use HTTP instead of HTTPS when it refers to the local machine (Default: <see langword="false"/>).
    /// </summary>
    /// <remarks>
    /// This option exists for local development only. Addresses that do not refer to the loopback interface always require HTTPS,
    /// because the access token must never be sent over an unencrypted connection.
    /// </remarks>
    public bool AllowHttpOnLoopback { get; set; }
}
