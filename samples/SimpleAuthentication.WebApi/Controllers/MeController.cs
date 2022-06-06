using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleAuthentication.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MeController : ControllerBase
{
    [Authorize]
    [HttpGet("authorize-bearer")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesDefaultResponseType]
    public ActionResult<User> GetWithBearer()
        => new User(User.Identity!.Name);

    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpGet("authorize-apikey")]
    public User GetWithApiKey()
        => new(User.Identity!.Name);

    [Authorize(AuthenticationSchemes = "Auth0")]
    [HttpGet("authorize-auth0")]
    public User GetWithAuth0()
    => new("Auth0 default user");
}

public record class User(string? UserName);