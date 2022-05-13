using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthenticationTools.JwtBearer;

namespace SimpleAuthenticationTools.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost]
    public IActionResult Login([FromServices] IJwtBearerGeneratorService jwtBearerGeneratorService)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.GivenName,"Donald"),
            new(ClaimTypes.Surname,"Duck")
        };

        var token = jwtBearerGeneratorService.CreateToken("marco", claims);
        return Ok(new { token });
    }
}
