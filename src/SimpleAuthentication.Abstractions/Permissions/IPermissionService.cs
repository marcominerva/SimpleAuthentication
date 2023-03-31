using System.Security.Claims;

namespace SimpleAuthentication.Permissions;

public interface IPermissionService
{
    Task<bool> IsGrantedAsync(ClaimsPrincipal user, IEnumerable<string> permissions);
}
