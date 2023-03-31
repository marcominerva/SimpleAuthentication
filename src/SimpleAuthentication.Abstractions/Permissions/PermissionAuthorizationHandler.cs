using Microsoft.AspNetCore.Authorization;

namespace SimpleAuthentication.Permissions;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService permissionService;

    public PermissionAuthorizationHandler(IPermissionService permissionService)
    {
        this.permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated ?? false)
        {
            var isGranted = await permissionService.IsGrantedAsync(context.User, requirement.Permissions);
            if (isGranted)
            {
                context.Succeed(requirement);
            }
        }
    }
}
