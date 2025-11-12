using System.Security.Claims;
using JwtBearerSample.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using SimpleAuthentication;
using SimpleAuthentication.JwtBearer;
using SimpleAuthentication.Permissions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

// Add authentication services.
builder.Services.AddSimpleAuthentication(builder.Configuration);

// Enable permission-based authorization.
builder.Services.AddScopePermissions(); // This is equivalent to builder.Services.AddPermissions<ScopeClaimPermissionHandler>();

// Define a custom handler for permission handling.
//builder.Services.AddPermissions<CustomPermissionHandler>();

builder.Services.AddAuthorizationBuilder()
    // Define permissions using a policy.
    .AddPolicy("PeopleRead", builder => builder.RequirePermission(Permissions.PeopleRead, Permissions.PeopleAdmin))
    //.AddPolicy("Bearer", builder => builder.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser())
    //.SetDefaultPolicy(new AuthorizationPolicyBuilder()
    //    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    //    .RequireAuthenticatedUser()
    //    .Build())
    //.SetFallbackPolicy(new AuthorizationPolicyBuilder()
    //    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    //    .RequireAuthenticatedUser()
    //    .Build())
    ;

// Uncomment the following line if you have multiple authentication schemes and
// you need to determine the authentication scheme at runtime (for example, you don't want to use the default authentication scheme).
//builder.Services.AddSingleton<IAuthenticationSchemeProvider, ApplicationAuthenticationSchemeProvider>();

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

builder.Services.AddOpenApi(options =>
{
    options.AddSimpleAuthentication(builder.Configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", builder.Environment.ApplicationName);
});

app.UseAuthentication();
app.UseAuthorization();

var authApiGroup = app.MapGroup("api/auth");

authApiGroup.MapPost("login", async (LoginRequest loginRequest, DateTime? expiration, IJwtBearerService jwtBearerService) =>
{
    // Check for login rights...

    // Add custom claims (optional).
    var claims = new List<Claim>
    {
        new(ClaimTypes.Role, "Administrator")
    };

    if (!string.IsNullOrWhiteSpace(loginRequest.Scopes))
    {
        claims.Add(new("scp", loginRequest.Scopes));
    }

    var token = await jwtBearerService.CreateTokenAsync(loginRequest.UserName, claims, absoluteExpiration: expiration);
    return TypedResults.Ok(new LoginResponse(token));
})
.WithDescription("Insert permissions in the scope property (for example: 'profile people:admin')");

authApiGroup.MapPost("validate", async Task<Results<Ok<User>, BadRequest>> (string token, bool validateLifetime, IJwtBearerService jwtBearerService) =>
{
    var result = await jwtBearerService.TryValidateTokenAsync(token, validateLifetime);

    if (!result.IsValid)
    {
        return TypedResults.BadRequest();
    }

    return TypedResults.Ok(new User(result.Principal.Identity!.Name));
})
.ProducesProblem(StatusCodes.Status400BadRequest);

authApiGroup.MapPost("refresh", async (string token, bool validateLifetime, DateTime? expiration, IJwtBearerService jwtBearerService) =>
{
    var newToken = await jwtBearerService.RefreshTokenAsync(token, validateLifetime, expiration);
    return TypedResults.Ok(new LoginResponse(newToken));
});

app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return TypedResults.Ok(new User(user.Identity!.Name));
})
.RequireAuthorization()
.RequirePermission("profile")
.WithDescription("This endpoint requires the 'profile' permission");

app.MapGet("api/people", () =>
{
    return TypedResults.NoContent();
})
.RequireAuthorization(policyNames: "PeopleRead")
.WithDescription($"This endpoint requires the '{Permissions.PeopleRead}' or '{Permissions.PeopleAdmin}' permissions");

app.Run();

public record class User(string? UserName);

public record class LoginRequest(string UserName, string Password, string? Scopes);

public record class LoginResponse(string Token);

public class CustomPermissionHandler : IPermissionHandler
{
    public Task<bool> IsGrantedAsync(ClaimsPrincipal user, IEnumerable<string> permissions)
    {
        bool isGranted;

        if (!permissions?.Any() ?? true)
        {
            isGranted = true;
        }
        else
        {
            var permissionClaim = user.FindFirstValue("permissions");
            var userPermissions = permissionClaim?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Enumerable.Empty<string>();

            isGranted = userPermissions.Intersect(permissions!).Any();
        }

        return Task.FromResult(isGranted);
    }
}

public static class Permissions
{
    public const string PeopleRead = "people:read";
    public const string PeopleWrite = "people:write";
    public const string PeopleAdmin = "people:admin";
}