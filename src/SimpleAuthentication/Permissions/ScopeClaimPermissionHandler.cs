using System.Security.Claims;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Checks for permissions reading the <em>scope</em> claim of the <seealso cref="ClaimsPrincipal"/> that represents the current user.
/// </summary>
/// <seealso cref="ClaimsPrincipal"/>
/// <seealso cref="Claim"/>
public class ScopeClaimPermissionHandler : IPermissionHandler
{
    private const string Scp = "scp";
    private const string Scope = "http://schemas.microsoft.com/identity/claims/scope";

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
            var scopeClaims = user.FindAll(Scp).Union(user.FindAll(Scope)).ToList();

            var scopes = scopeClaims.SelectMany(s => s.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            isGranted = scopes.Intersect(permissions!).Any();
        }

        return Task.FromResult(isGranted);
    }
}
