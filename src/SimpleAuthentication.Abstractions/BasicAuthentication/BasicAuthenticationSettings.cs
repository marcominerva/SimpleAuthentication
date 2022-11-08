using Microsoft.AspNetCore.Authentication;

namespace SimpleAuthentication.BasicAuthentication;

/// <summary>
/// Options class provides information needed to control Basic Authentication handler behavior.
/// </summary>
/// <seealso cref="AuthenticationSchemeOptions"/>
public class BasicAuthenticationSettings : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the authentication scheme name (Default: Basic).
    /// </summary>
    public string SchemeName { get; set; } = BasicAuthenticationDefaults.AuthenticationScheme;

    /// <summary>
    /// Gets or sets a fixed value to compare the user name against.
    /// If you need to perform custom checks to validate the authentication, you should leave this value to <see langword="null"/>, as well as <see cref="Password"/>, and register an <see cref="IBasicAuthenticationValidator"/> service.
    /// </summary>
    /// <seealso cref="Password"/>
    /// <seealso cref="IBasicAuthenticationValidator"/>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets a fixed value to compare the password against.
    /// If you need to perform custom checks to validate the authentication, you should leave this value to <see langword="null"/>, as well as <see cref="UserName"/>, and register an <see cref="IBasicAuthenticationValidator"/> service.
    /// </summary>
    /// <seealso cref="UserName"/>
    /// <seealso cref="IBasicAuthenticationValidator"/>
    public string? Password { get; set; }

    /// <summary>
    /// Internal value that tells if Basic authentication is actually enabled.
    /// </summary>
    internal bool IsEnabled { get; set; }
}
