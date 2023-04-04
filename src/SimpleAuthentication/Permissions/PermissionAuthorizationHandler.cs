using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionHandler permissionHandler;

    public PermissionAuthorizationHandler(IPermissionHandler permissionHandler)
    {
        ArgumentNullException.ThrowIfNull(permissionHandler);

        this.permissionHandler = permissionHandler;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var isGranted = await permissionHandler.IsGrantedAsync(context.User, requirement.Permissions);
        if (isGranted)
        {
            context.Succeed(requirement);
        }
    }
}
