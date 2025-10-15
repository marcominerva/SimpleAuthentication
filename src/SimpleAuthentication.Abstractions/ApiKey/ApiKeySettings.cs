using System.Security.Claims;
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

    /// <summary>
    /// Gets or sets the optional list of roles to assign to the user when using <see cref="ApiKeyValue"/> and <see cref="UserName"/>.
    /// </summary>
    /// <seealso cref="ApiKeyValue"/>
    /// <seealso cref="UserName"/>
    public IEnumerable<string> Roles { get; set; } = [];

    /// <summary>
    /// The collection of valid API keys.
    /// </summary>
    /// <seealso cref="ApiKey"/>
    public IEnumerable<ApiKey> ApiKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets a <see cref="string"/> that defines the <see cref="ClaimsIdentity.NameClaimType"/>.
    /// </summary>
    /// <remarks>
    /// Controls the value <see cref="ClaimsIdentity.Name"/> returns. It will return the first <see cref="Claim.Value"/> where the <see cref="Claim.Type"/> equals <see cref="NameClaimType"/>.
    /// The default is <see cref="ClaimsIdentity.DefaultNameClaimType"/>.
    /// </remarks>
    public string NameClaimType { get; set; } = ClaimsIdentity.DefaultNameClaimType;

    /// <summary>
    /// Gets or sets the <see cref="string"/> that defines the <see cref="ClaimsIdentity.RoleClaimType"/>.
    /// </summary>
    /// <remarks>
    /// <para>Controls the results of <see cref="ClaimsPrincipal.IsInRole( string )"/>.</para>
    /// <para>Each <see cref="Claim"/> where <see cref="Claim.Type"/> == <see cref="RoleClaimType"/> will be checked for a match against the 'string' passed to <see cref="ClaimsPrincipal.IsInRole(string)"/>.</para>
    /// The default is <see cref="ClaimsIdentity.DefaultRoleClaimType"/>.
    /// </remarks>
    public string RoleClaimType { get; set; } = ClaimsIdentity.DefaultRoleClaimType;

    internal IEnumerable<ApiKey> GetAllApiKeys()
    {
        var apiKeys = (ApiKeys ?? []).ToHashSet();
        if (!string.IsNullOrWhiteSpace(ApiKeyValue) && !string.IsNullOrWhiteSpace(UserName))
        {
            // If necessary, add the API Key from the base properties.
            apiKeys.Add(new(ApiKeyValue, UserName, Roles));
        }

        return apiKeys;
    }
}
