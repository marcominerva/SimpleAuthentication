using System.Security.Claims;
using JwtBearerSample.Authentication;
using JwtBearerSample.Swagger;
using Microsoft.AspNetCore.Authentication;
using SimpleAuthentication;
using SimpleAuthentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddSimpleAuthentication(builder.Configuration);

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
    options.ParameterFilter<DateTimeParameterFilter>();

    options.AddSimpleAuthentication(builder.Configuration);
});

var app = builder.Build();
app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthenticationAndAuthorization();

var authApiGroup = app.MapGroup("api/auth");

authApiGroup.MapPost("login", (LoginRequest loginRequest, DateTime? expiration, IJwtBearerService jwtBearerService) =>
{
    // Check for login rights...

    // Add custom claims (optional).
    var claims = new List<Claim>
    {
        new(ClaimTypes.GivenName, "Marco"),
        new(ClaimTypes.Surname, "Minerva")
    };

    var token = jwtBearerService.CreateToken(loginRequest.UserName, claims, absoluteExpiration: expiration);
    return Results.Ok(new LoginResponse(token));
});

authApiGroup.MapPost("validate", (string token, bool validateLifetime, IJwtBearerService jwtBearerService) =>
{
    var isValid = jwtBearerService.TryValidateToken(token, validateLifetime, out var claimsPrincipal);
    if (!isValid)
    {
        return Results.BadRequest();
    }

    return Results.Ok(new User(claimsPrincipal!.Identity!.Name));
});

authApiGroup.MapPost("refresh", (string token, bool validateLifetime, DateTime? expiration, IJwtBearerService jwtBearerService) =>
{
    var newToken = jwtBearerService.RefreshToken(token, validateLifetime, expiration);
    return Results.Ok(new LoginResponse(newToken));
});

app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return new User(user.Identity!.Name);
})
.RequireAuthorization();

app.Run();

public record class User(string? UserName);

public record class LoginRequest(string UserName, string Password);

public record class LoginResponse(string Token);