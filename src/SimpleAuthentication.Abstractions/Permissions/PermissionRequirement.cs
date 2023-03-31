using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

public class PermissionRequirement : IAuthorizationRequirement
{
    public IEnumerable<string> Permissions { get; }

    public PermissionRequirement(string permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        Permissions = permissions.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
