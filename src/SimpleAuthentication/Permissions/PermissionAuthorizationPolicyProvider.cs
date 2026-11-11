using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SimpleAuthentication.Permissions;

/// <summary>
/// Resolves explicitly registered authorization policies and dynamically creates permission-based policies when no match is found.
/// </summary>
/// <remarks>
/// An unknown policy name is treated as a permission name and converted into an <see cref="AuthorizationPolicy"/>
/// containing a <see cref="PermissionRequirement"/> for that permission.
/// </remarks>
internal class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Check if the policy is already registered in the options.
        var policy = await base.GetPolicyAsync(policyName);

        // If not, create a new policy with a PermissionRequirement for the given policy name.
        policy ??= new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(policyName)).Build();

        return policy;
    }
}
