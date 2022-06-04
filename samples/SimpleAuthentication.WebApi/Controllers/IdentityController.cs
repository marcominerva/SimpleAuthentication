using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.JwtBearer;

namespace SimpleAuthentication.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController : ControllerBase
{
    private readonly IJwtBearerService jwtBearerService;

    public AuthController(IJwtBearerService jwtBearerService)
    {
        this.jwtBearerService = jwtBearerService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public ActionResult<LoginResponse> Login(LoginRequest loginRequest, DateTime? expiration = null)
    {
        // Check for login rights...

        // Add custom claims (optional).
        var claims = new List<Claim>
        {
            new(ClaimTypes.GivenName, "Marco"),
            new(ClaimTypes.Surname, "Minerva")
        };

        var token = jwtBearerService.CreateToken(loginRequest.UserName, claims, absoluteExpiration: expiration);
        return new LoginResponse(token);
    }

    [HttpPost("validate")]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public ActionResult<User> Validate(string token, bool validateLifetime = true)
    {
        var isValid = jwtBearerService.TryValidateToken(token, validateLifetime, out var claimsPrincipal);
        if (!isValid)
        {
            return BadRequest();
        }

        return new User(claimsPrincipal!.Identity!.Name);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public ActionResult<LoginResponse> Refresh(string token, bool validateLifetime = true, DateTime? expiration = null)
    {
        var newToken = jwtBearerService.RefreshToken(token, validateLifetime, expiration);
        return new LoginResponse(newToken);
    }
}

public record class LoginRequest(string UserName, string Password);

public record class LoginResponse(string Token);

public record class ValidationResponse(bool IsValid, User? User);