using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

internal class PermissionAuthorizationHandler(IPermissionHandler permissionHandler) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var isGranted = await permissionHandler.IsGrantedAsync(context.User, requirement.Permissions);
        if (isGranted)
        {
            context.Succeed(requirement);
        }
    }
}
