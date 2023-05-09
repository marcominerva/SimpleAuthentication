using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Contains the list of permissions that defines the requirement.
/// </summary>
/// <seealso cref="IAuthorizationRequirement"/>
public class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the list of permissions
    /// </summary>
    public IEnumerable<string> Permissions { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="PermissionRequirement"/> class using a comma separated string of permissions.
    /// </summary>
    /// <param name="permissions">A comma separated string of permissions.</param>
    public PermissionRequirement(string permissions)
        : this(permissions.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="PermissionRequirement"/> class using a list of permissions.
    /// </summary>
    /// <param name="permissions">The permissions list.</param>
    public PermissionRequirement(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Permissions = permissions;
    }
}
