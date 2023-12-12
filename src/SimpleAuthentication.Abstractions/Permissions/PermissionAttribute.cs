using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Specifies that the class or method that this attribute is applied to requires the specified authorization based on permissions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PermissionAttribute"/> class with the specified permissions.
/// </remarks>
/// <param name="permissions">The permission list to require for authorization.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PermissionAttribute(params string[] permissions) : AuthorizeAttribute(string.Join(",", permissions))
{
}
