using System.Security.Claims;
using BasicAuthenticationSample.Authentication;
using Microsoft.AspNetCore.Authentication;
using SimpleAuthentication;
using SimpleAuthentication.BasicAuthentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

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