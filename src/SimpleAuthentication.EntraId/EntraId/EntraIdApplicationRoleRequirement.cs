using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.EntraId;

/// <summary>
/// Requires an app-only access token, issued by Microsoft Entra ID and validated by a specific authentication scheme,
/// that contains at least one of the allowed application roles.
/// </summary>
/// <remarks>
/// <para>
/// Only the identities created by <see cref="SchemeName"/> are evaluated. In this way, claims that come from other authentication
/// schemes (for example a locally issued JWT that contains a <c>roles</c> claim) can never satisfy the requirement, even when the
/// authorization policy accepts more than one scheme.
/// </para>
/// <para>
/// A token is considered app-only when it contains the <c>idtyp</c> claim with the <c>app</c> value and no delegated scopes.
/// Tokens that are issued on behalf of a user are rejected, even if the user has been assigned to one of the allowed roles.
/// The <c>idtyp</c> claim is optional and must be explicitly added to the access tokens of the API: tokens without it are rejected.
/// See <see href="https://learn.microsoft.com/entra/identity-platform/optional-claims-reference">Optional claims reference</see> for more information.
/// </para>
/// </remarks>
/// <seealso cref="IAuthorizationRequirement"/>
public class EntraIdApplicationRoleRequirement : AuthorizationHandler<EntraIdApplicationRoleRequirement>, IAuthorizationRequirement
{
    private const string IdentityTypeClaimType = "idtyp";
    private const string ApplicationIdentityType = "app";
    private const string RolesClaimType = "roles";
    private const string ScpClaimType = "scp";
    private const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";

    private readonly HashSet<string> roles;

    /// <summary>
    /// Gets the name of the authentication scheme that must have created the identity that holds the application roles.
    /// </summary>
    public string SchemeName { get; }

    /// <summary>
    /// Gets the application roles that satisfy the requirement. The token must contain at least one of them.
    /// </summary>
    public IEnumerable<string> AllowedRoles => roles;

    /// <summary>
    /// Creates a new instance of the <see cref="EntraIdApplicationRoleRequirement"/> class.
    /// </summary>
    /// <param name="schemeName">The name of the Microsoft Entra ID authentication scheme.</param>
    /// <param name="allowedRoles">The application roles that satisfy the requirement.</param>
    /// <exception cref="ArgumentNullException"><paramref name="allowedRoles"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="schemeName"/> is empty, or <paramref name="allowedRoles"/> is empty or contains empty values.</exception>
    public EntraIdApplicationRoleRequirement(string schemeName, IEnumerable<string> allowedRoles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemeName);
        ArgumentNullException.ThrowIfNull(allowedRoles);

        roles = allowedRoles.ToHashSet(StringComparer.Ordinal);
        if (roles.Count == 0 || roles.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one application role must be specified, and roles cannot be empty.", nameof(allowedRoles));
        }

        SchemeName = schemeName;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EntraIdApplicationRoleRequirement requirement)
    {
        if (context.User.Identities.Any(requirement.IsSatisfiedBy))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override string ToString()
        => $"{nameof(EntraIdApplicationRoleRequirement)}:The '{SchemeName}' scheme must provide an app-only token with one of the following roles: ({string.Join("|", roles)})";

    private bool IsSatisfiedBy(ClaimsIdentity identity)
        => identity.IsAuthenticated
            && string.Equals(identity.AuthenticationType, SchemeName, StringComparison.Ordinal)
            && identity.HasClaim(IdentityTypeClaimType, ApplicationIdentityType)
            && !identity.HasClaim(claim => claim.Type is ScpClaimType or ScopeClaimType)
            && identity.HasClaim(claim => claim.Type is RolesClaimType or ClaimTypes.Role && roles.Contains(claim.Value));
}
