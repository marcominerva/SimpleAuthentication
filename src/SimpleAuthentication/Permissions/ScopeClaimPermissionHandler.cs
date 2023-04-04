using System.Security.Claims;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Checks for permissions reading the claim <em>scope</em> of the <seealso cref="ClaimsPrincipal"/> that represents the current user.
/// </summary>
/// <seealso cref="ClaimsPrincipal"/>
/// <seealso cref="Claim"/>
public class ScopeClaimPermissionHandler : IPermissionHandler
{
    /// <inheritdoc/>
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
