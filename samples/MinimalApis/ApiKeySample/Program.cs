using System.Security.Claims;
using ApiKeySample.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SimpleAuthentication;
using SimpleAuthentication.ApiKey;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

// Add authentication services.
builder.Services.AddSimpleAuthentication(builder.Configuration);

//builder.Services.AddAuthorizationBuilder()
//    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
//        .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
//        .RequireAuthenticatedUser()
//        .Build())
//    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
//        .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
//        .RequireAuthenticatedUser()
//        .Build())
//    .AddPolicy("ApiKey", builder => builder.AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme).RequireAuthenticatedUser());

// This service is used when there isn't a fixed API Key value in the configuration.
builder.Services.AddTransient<IApiKeyValidator, CustomApiKeyValidator>();

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

app.MapGet("api/me", (ClaimsPrincipal user, IOptions<ApiKeySettings> options) =>
{
    var roles = user.FindAll(options.Value.RoleClaimType).Select(c => c.Value);
    return TypedResults.Ok(new User(user.Identity!.Name, roles));
})
.RequireAuthorization();

app.MapGet("api/administrator", () => TypedResults.NoContent())
.WithDescription("This endpoint requires the user to have the 'Administrator' role.")
.RequireAuthorization(policy => policy.RequireRole("Administrator"));

app.MapGet("api/user", () => TypedResults.NoContent())
.WithDescription("This endpoint requires the user to have the 'User' role.")
.RequireAuthorization(policy => policy.RequireRole("User"));

app.Run();

public record class User(string? UserName, IEnumerable<string> Roles);

public class CustomApiKeyValidator : IApiKeyValidator
{
    public Task<ApiKeyValidationResult> ValidateAsync(string apiKey)
    {
        var result = apiKey switch
        {
            "ArAilHVOoL3upX78Cohq" => ApiKeyValidationResult.Success("User 1"),
            "DiUU5EqImTYkxPDAxBVS" => ApiKeyValidationResult.Success("User 2"),
            _ => ApiKeyValidationResult.Fail("Invalid User")
        };

        return Task.FromResult(result);
    }
}