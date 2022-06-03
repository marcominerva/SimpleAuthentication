using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.JwtBearer;

namespace SimpleAuthentication.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public LoginResponse Login(LoginRequest loginRequest, [FromServices] IJwtBearerService jwtBearerService)
    {
        // Check for login rights...

        // Add custom claims (optional).
        var claims = new List<Claim>
        {
            new(ClaimTypes.GivenName, "Marco"),
            new(ClaimTypes.Surname, "Minerva")
        };

        var token = jwtBearerService.CreateToken(loginRequest.Username, claims);
        return new(token);
    }
}

public record class LoginRequest(string Username, string Password);

public record class LoginResponse(string Token);