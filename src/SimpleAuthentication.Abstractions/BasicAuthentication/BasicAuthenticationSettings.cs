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

    private ICollection<Credential> credentials = new HashSet<Credential>();
    /// <summary>
    /// The collection of authorization credentials.
    /// </summary>
    /// <seealso cref="Credential"/>
    public ICollection<Credential> Credentials
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password))
            {
                // If necessary, add the credentials from the base properties.
                credentials.Add(new Credential(UserName, Password));
            }

            return credentials;
        }

        internal set => credentials = value ?? new HashSet<Credential>();
    }

    internal bool IsConfigured { get; set; }
}

/// <summary>
/// Store credentials used for Basic Authentication.
/// </summary>
/// <param name="UserName">The user name</param>
/// <param name="Password">The password</param>
public record class Credential(string UserName, string Password);
