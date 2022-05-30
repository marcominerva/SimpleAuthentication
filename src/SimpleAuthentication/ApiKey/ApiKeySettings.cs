using Microsoft.AspNetCore.Authentication;

namespace SimpleAuthentication.ApiKey;

/// <summary>
/// Options class provides information needed to control ApiKey Authentication handler behavior
/// </summary>
/// <seealso cref="AuthenticationSchemeOptions"/>
public class ApiKeySettings : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the authentication scheme name (Default: ApiKey).
    /// </summary>
    public string SchemeName { get; set; } = "ApiKey";

    /// <summary>
    /// Gets or sets the name of the header that contains the ApiKey.
    /// </summary>
    /// <remarks>You can specify also the <see cref="ApiKeySettings.QueryName"/> to support both header and query string ApiKey Authentication</remarks>
    /// <seealso cref="ApiKeySettings.QueryName"/>
    public string? HeaderName { get; set; }

    /// <summary>
    /// Gets or sets the name of the query string parameter that contains the ApiKey.
    /// </summary>
    /// <remarks>You can specify also the <see cref="ApiKeySettings.HeaderName"/> to support both header and query string ApiKey Authentication</remarks>
    /// <seealso cref="ApiKeySettings.HeaderName"/>
    public string? QueryName { get; set; }

    public string? ApiKeyValue { get; set; }

    public string? DefaultUsername { get; set; }
}
