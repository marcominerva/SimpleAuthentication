using System.Security.Claims;
using BasicAuthenticationSample.Authentication;
using Microsoft.AspNetCore.Authentication;
using SimpleAuthentication;
using SimpleAuthentication.BasicAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();

builder.Services.AddSimpleAuthentication(builder.Configuration);

//builder.Services.AddAuthorization(options =>
//{
//    options.FallbackPolicy = options.DefaultPolicy = new AuthorizationPolicyBuilder()
//                                .AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme)
//                                .RequireAuthenticatedUser()
//                                .Build();

//    options.AddPolicy("Basic", policy => policy
//                                .AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme)
//                                .RequireAuthenticatedUser());
//});

builder.Services.AddTransient<IBasicAuthenticationValidator, CustomBasicAuthenticationValidator>();

// Uncomment the following line if you have multiple authentication schemes and
// you need to determine the authentication scheme at runtime (for example, you don't want to use the default authentication scheme).
//builder.Services.AddSingleton<IAuthenticationSchemeProvider, ApplicationAuthenticationSchemeProvider>();

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
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

// The following middlewares aren't strictly necessary in .NET 7.0, because they are automatically added when detecting
// that the corresponding services have been registered. However, you may need to call them explicitly if the default
// middlewares configuration is not correct for your app, for example when you need to use CORS.
// Check https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/middleware for more information.
//app.UseAuthentication();
//app.UseAuthorization();

app.MapGet("api/me", (ClaimsPrincipal user) =>
{
    return new User(user.Identity!.Name);
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