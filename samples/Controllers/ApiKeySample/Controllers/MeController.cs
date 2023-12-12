using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public ActionResult<User> Get()
        => new User(User.Identity!.Name);
}

public record class User(string? UserName);