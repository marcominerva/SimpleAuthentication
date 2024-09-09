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
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest, DateTime? expiration = null)
    {
        // Check for login rights...

        // Add custom claims (optional).
        var claims = new List<Claim>();
        if (loginRequest.Scopes?.Any() ?? false)
        {
            claims.Add(new("scp", loginRequest.Scopes));
        }

        var token = await jwtBearerService.CreateTokenAsync(loginRequest.UserName, claims, absoluteExpiration: expiration);
        return new LoginResponse(token);
    }

    [HttpPost("validate")]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<User>> Validate(string token, bool validateLifetime = true)
    {
        var result = await jwtBearerService.TryValidateTokenAsync(token, validateLifetime);

        if (!result.IsValid)
        {
            return BadRequest();
        }

        return new User(result.Principal.Identity!.Name);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<LoginResponse>> Refresh(string token, bool validateLifetime = true, DateTime? expiration = null)
    {
        var newToken = await jwtBearerService.RefreshTokenAsync(token, validateLifetime, expiration);
        return new LoginResponse(newToken);
    }
}

public record class LoginRequest(string UserName, string Password, string? Scopes);

public record class LoginResponse(string Token);