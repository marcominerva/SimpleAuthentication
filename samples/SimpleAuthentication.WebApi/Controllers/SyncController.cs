using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleAuthentication.WebApi.Controllers;

[ApiController]
[Route("service/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class SyncController : ControllerBase
{
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpGet("authorize-apikey")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesDefaultResponseType]
    public ActionResult<User> GetWithApiKey()
        => new User(User.Identity!.Name);
}
