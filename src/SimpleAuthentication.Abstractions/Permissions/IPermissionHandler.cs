using System.Security.Claims;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Classes implementing this interface are able to make a decision if permission-based authorization is allowed.
/// </summary>
public interface IPermissionHandler
{
    /// <summary>
    /// Checks if the given <paramref name="user"/> owns the specified <paramref name="permissions"/>.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="permissions">The list of permissions that grant access.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <seealso cref="ClaimsPrincipal"/>
    /// <returns><see langword="true"/> if <paramref name="user"/> owns the specified <paramref name="permissions"/>; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsGrantedAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default);
}
