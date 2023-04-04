using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.Permissions;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtBearerSample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MeController : ControllerBase
{
    [Authorize]
    [Permissions("profile")]
    [HttpGet]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(description: "This endpoint requires the 'profile' permission")]
    public ActionResult<User> Get()
        => new User(User.Identity!.Name);
}

public record class User(string? UserName);