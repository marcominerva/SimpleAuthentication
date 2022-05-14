using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimpleAuthenticationTools.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public User Get()
        => new(User.Identity!.Name);
}

public record class User(string? Username);