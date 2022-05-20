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
    public User GetWithBearer()
        => new(User.Identity!.Name);

    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpGet("authorize-apikey")]
    public User GetWithApiKey()
        => new(User.Identity!.Name);
}

public record class User(string? Username);