using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Specifies that the class or method that this attribute is applied to requires the specified authorization based on permissions.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionAttribute"/> class with the specified permissions.
    /// </summary>
    /// <param name="permissions">The permission list to require for authorization.</param>
    public PermissionAttribute(params string[] permissions)
        : base(string.Join(",", permissions))
    {
    }
}
