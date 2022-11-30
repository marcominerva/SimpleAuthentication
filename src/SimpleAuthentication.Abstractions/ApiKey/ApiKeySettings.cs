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
    public string SchemeName { get; set; } = ApiKeyDefaults.AuthenticationScheme;

    /// <summary>
    /// Gets or sets the name of the header that contains the API key.
    /// </summary>
    /// <remarks>You can specify also the <see cref="QueryStringKey"/> to support both header and query string authentication</remarks>
    /// <seealso cref="QueryStringKey"/>
    public string? HeaderName { get; set; }

    /// <summary>
    /// Gets or sets the name of the query string parameter that contains the API key.
    /// </summary>
    /// <remarks>You can specify also the <see cref="HeaderName"/> to support both header and query string authentication</remarks>
    /// <seealso cref="HeaderName"/>
    public string? QueryStringKey { get; set; }

    /// <summary>
    /// Gets or sets a fixed value to compare the API key against. If you need to perform custom checks to validate the API key, you should leave this value to <see langword="null"/> and register an <see cref="IApiKeyValidator"/> service.
    /// </summary>
    /// <seealso cref="UserName"/>
    /// <seealso cref="IApiKeyValidator"/>
    public string? ApiKeyValue { get; set; }

    /// <summary>
    /// Gets or sets the user name of to be used if the <see cref="ApiKeyValue"/> property is set.
    /// </summary>
    /// <seealso cref="ApiKeyValue"/>
    public string? UserName { get; set; }
}
