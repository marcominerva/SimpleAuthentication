using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SimpleAuthentication.Permissions;

internal class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var policy = await base.GetPolicyAsync(policyName);
        policy ??= new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(policyName)).Build();

        return policy;
    }
}
