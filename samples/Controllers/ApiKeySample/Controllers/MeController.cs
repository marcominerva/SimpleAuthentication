using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SimpleAuthentication.ApiKey;

namespace ApiKeySample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MeController : ControllerBase
{
    [Authorize]
    [HttpGet]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public ActionResult<User> Get(IOptions<ApiKeySettings> apiKeySettingsOptions)
    {
        // Get roles using the configured role claim type from options (default is ClaimTypes.Role)
        var roles = User.FindAll(apiKeySettingsOptions.Value.RoleClaimType).Select(c => c.Value);

        return new User(User.Identity!.Name, roles);
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointDescription("This endpoint requires the user to have the 'Administrator' role")]
    public IActionResult AdministratorOnly()
        => NoContent();

    [Authorize(Roles = "User")]
    [HttpGet("user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointDescription("This endpoint requires the user to have the 'User' role")]
    public IActionResult UserOnly()
        => NoContent();
}

public record class User(string? UserName, IEnumerable<string> Roles);