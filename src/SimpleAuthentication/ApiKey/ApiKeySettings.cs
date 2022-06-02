using Microsoft.AspNetCore.Authentication;

namespace SimpleAuthentication.ApiKey;

/// <summary>
/// Options class provides information needed to control API key Authentication handler behavior.
/// </summary>
/// <seealso cref="AuthenticationSchemeOptions"/>
public class ApiKeySettings : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the authentication scheme name (Default: ApiKey).
    /// </summary>
    public string SchemeName { get; set; } = "ApiKey";

    /// <summary>
    /// Gets or sets the name of the header that contains the API key.
    /// </summary>
    /// <remarks>You can specify also the <see cref="QueryName"/> to support both header and query string authentication</remarks>
    /// <seealso cref="QueryName"/>
    public string? HeaderName { get; set; }

    /// <summary>
    /// Gets or sets the name of the query string parameter that contains the API key.
    /// </summary>
    /// <remarks>You can specify also the <see cref="HeaderName"/> to support both header and query string authentication</remarks>
    /// <seealso cref="HeaderName"/>
    public string? QueryName { get; set; }

    /// <summary>
    /// Gets or sets a fixed value to compare the API key against. If you need to perform custom checks to validate the API key, you should leave this value <see langword="null"/> and register an <see cref="IApiKeyValidator"/> service.
    /// </summary>
    /// <seealso cref="DefaultUsername"/>
    /// <seealso cref="IApiKeyValidator"/>
    public string? ApiKeyValue { get; set; }

    /// <summary>
    /// Gets or sets the Username of to be used if the <see cref="ApiKeyValue"/> property is set.
    /// </summary>
    /// <seealso cref="ApiKeyValue"/>
    public string? DefaultUsername { get; set; }
}
