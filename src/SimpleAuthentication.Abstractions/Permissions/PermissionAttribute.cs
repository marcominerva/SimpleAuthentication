using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(params string[] permissions)
        : base(string.Join(",", permissions))
    {
    }
}
