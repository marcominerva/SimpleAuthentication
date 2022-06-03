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
    public User GetWithApiKey()
        => new(User.Identity!.Name);
}
