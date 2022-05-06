using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SimpleAuthentication.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost]
    public IActionResult Login([FromServices] IJwtTokenGeneratorService tokenGeneratorService)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.GivenName,"Donald"),
            new(ClaimTypes.Surname,"Duck")
        };

        var token = tokenGeneratorService.CreateToken("marco", claims);
        return Ok(new { token });
    }
}
