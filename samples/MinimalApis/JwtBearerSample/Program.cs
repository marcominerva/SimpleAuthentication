using System.Security.Claims;
using JwtBearerSample.Authentication;
using JwtBearerSample.Swagger;
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
builder.Services.AddPermissions<ScopeClaimPermissionHandler>();

//builder.Services.AddAuthorization(options =>
//{
//    options.FallbackPolicy = options.DefaultPolicy = new AuthorizationPolicyBuilder()
//                                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
//                                .RequireAuthenticatedUser()
//                                .Build();

//    options.AddPolicy("Bearer", policy => policy
//                                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
//                                .RequireAuthenticatedUser());
//});

// Uncomment the following line if you have multiple authentication schemes and
// you need to determine the authentication scheme at runtime (for example, you don't want to use the default authentication scheme).
//builder.Services.AddSingleton<IAuthenticationSchemeProvider, ApplicationAuthenticationSchemeProvider>();

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<MissingSchemasOperationFilter>();

    options.AddSimpleAuthentication(builder.Configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

var authApiGroup = app.MapGroup("api/auth");

authApiGroup.MapPost("login", (LoginRequest loginRequest, DateTime? expiration, IJwtBearerService jwtBearerService) =>
{
    // Check for login rights...

    // Add custom claims (optional).
    var claims = new List<Claim>();
    if (loginRequest.Scopes?.Any() ?? false)
    {
        claims.Add(new("scope", loginRequest.Scopes));
    }

    var token = jwtBearerService.CreateToken(loginRequest.UserName, claims, absoluteExpiration: expiration);
    return TypedResults.Ok(new LoginResponse(token));
})
.WithOpenApi(operation =>
{
    operation.Description = "Insert permissions in the scope property (for example: 'profile people_admin')";
    return operation;
});

authApiGroup.MapPost("validate", Results<Ok<User>, BadRequest> (string token, bool validateLifetime, IJwtBearerService jwtBearerService) =>
{
    var isValid = jwtBearerService.TryValidateToken(token, validateLifetime, out var claimsPrincipal);
    if (!isValid)
    {
        return TypedResults.BadRequest();
    }

    return TypedResults.Ok(new User(claimsPrincipal!.Identity!.Name));
})
.WithOpenApi();

authApiGroup.MapPost("refresh", (string token, bool validateLifetime, DateTime? expiration, IJwtBearerService jwtBearerService) =>
{
    var newToken = jwtBearerService.RefreshToken(token, validateLifetime, expiration);
    return TypedResults.Ok(new LoginResponse(newToken));
})
.WithOpenApi();

app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return TypedResults.Ok(new User(user.Identity!.Name));
})
.RequireAuthorization()
.RequirePermissions("profile")
.WithOpenApi(operation =>
{
    operation.Description = "This endpoint requires the 'profile' permission";
    return operation;
});

app.Run();

public record class User(string? UserName);

public record class LoginRequest(string UserName, string Password, string Scopes);

public record class LoginResponse(string Token);