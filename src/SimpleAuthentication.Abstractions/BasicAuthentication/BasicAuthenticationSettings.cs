using System.Security.Claims;
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
    /// Gets or sets the optional list of roles to assign to the user when using <see cref="UserName"/> and <see cref="Password"/>.
    /// </summary>
    /// <seealso cref="UserName"/>
    /// <seealso cref="Password"/>
    public string[]? Roles { get; set; }

    private ICollection<Credential> credentials = [];
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
                credentials.Add(new Credential(UserName, Password, Roles));
            }

            return credentials;
        }

        internal set => credentials = value ?? [];
    }

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

}

/// <summary>
/// Store credentials used for Basic Authentication.
/// </summary>
/// <param name="UserName">The user name</param>
/// <param name="Password">The password</param>
/// <param name="Roles">The optional list of roles to assign to the user</param>
public record class Credential(string UserName, string Password, string[]? Roles = null);
