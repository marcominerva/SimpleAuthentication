using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SimpleAuthentication.JwtBearer;
using Swashbuckle.AspNetCore.Annotations;

namespace JwtBearerSample.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController(IJwtBearerService jwtBearerService) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    [SwaggerOperation(description: "Insert permissions in the scope property (for example: 'profile people:admin')")]
    public ActionResult<LoginResponse> Login(LoginRequest loginRequest, DateTime? expiration = null)
    {
        // Check for login rights...

        // Add custom claims (optional).
        var claims = new List<Claim>();
        if (loginRequest.Scopes?.Any() ?? false)
        {
            claims.Add(new("scp", loginRequest.Scopes));
        }

        var token = jwtBearerService.CreateToken(loginRequest.UserName, claims, absoluteExpiration: expiration);
        return new LoginResponse(token);
    }

    [HttpPost("validate")]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public ActionResult<User> Validate(string token, bool validateLifetime = true)
    {
        if (jwtBearerService.TryValidateToken(token, validateLifetime, out var claimsPrincipal))
        {
            return new User(claimsPrincipal.Identity!.Name);
        }

        return BadRequest();
    }

    [HttpPost("refresh")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public ActionResult<LoginResponse> Refresh(string token, bool validateLifetime = true, DateTime? expiration = null)
    {
        var newToken = jwtBearerService.RefreshToken(token, validateLifetime, expiration);
        return new LoginResponse(newToken);
    }
}

public record class LoginRequest(string UserName, string Password, string? Scopes);

public record class LoginResponse(string Token);