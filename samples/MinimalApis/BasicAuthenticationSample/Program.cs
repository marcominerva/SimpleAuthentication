using System.Security.Claims;
using BasicAuthenticationSample.Authentication;
using Microsoft.AspNetCore.Authentication;
using SimpleAuthentication;
using SimpleAuthentication.BasicAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

// Add authentication services.
builder.Services.AddSimpleAuthentication(builder.Configuration);

//builder.Services.AddAuthorizationBuilder()
//    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
//        .AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme)
//        .RequireAuthenticatedUser()
//        .Build())
//    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
//        .AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme)
//        .RequireAuthenticatedUser()
//        .Build())
//    .AddPolicy("Basic", builder => builder.AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme).RequireAuthenticatedUser());

builder.Services.AddTransient<IBasicAuthenticationValidator, CustomBasicAuthenticationValidator>();

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
    options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return TypedResults.Ok(new User(user.Identity!.Name));
})
.RequireAuthorization()
.WithOpenApi();

app.Run();

public record class User(string? UserName);

public class CustomBasicAuthenticationValidator : IBasicAuthenticationValidator
{
    public Task<BasicAuthenticationValidationResult> ValidateAsync(string userName, string password)
    {
        if (userName == password)
        {
            var claims = new List<Claim>() { new(ClaimTypes.Role, "User") };
            return Task.FromResult(BasicAuthenticationValidationResult.Success(userName, claims));
        }

        return Task.FromResult(BasicAuthenticationValidationResult.Fail("Invalid user"));
    }
}