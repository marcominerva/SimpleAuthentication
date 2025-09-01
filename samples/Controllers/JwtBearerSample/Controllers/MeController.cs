using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.Permissions;

namespace JwtBearerSample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MeController : ControllerBase
{
    [Authorize]
    [Permission("profile")]
    [HttpGet]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    [EndpointDescription(description: "This endpoint requires the 'profile' permission")]
    public ActionResult<User> Get()
        => new User(User.Identity!.Name);
}

public record class User(string? UserName);