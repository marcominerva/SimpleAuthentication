using System.Security.Claims;

namespace SimpleAuthentication.Permissions;

public class ScopeClaimPermissionService : IPermissionService
{
    public Task<bool> IsGrantedAsync(ClaimsPrincipal user, IEnumerable<string> permissions)
    {
        bool isGranted;

        if (!permissions?.Any() ?? true)
        {
            isGranted = true;
        }
        else
        {
            var scopeClaim = user.FindFirstValue("scope");
            var scopes = scopeClaim?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();

            isGranted = scopes.Intersect(permissions!).Any();
        }

        return Task.FromResult(isGranted);
    }
}
