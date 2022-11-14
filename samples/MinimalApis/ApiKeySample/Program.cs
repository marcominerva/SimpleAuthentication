using System.Security.Claims;
using ApiKeySample.Authentication;
using Microsoft.AspNetCore.Authentication;
using SimpleAuthentication;
using SimpleAuthentication.ApiKey;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddSimpleAuthentication(builder.Configuration);

//builder.Services.AddAuthorization(options =>
//{
//    options.FallbackPolicy = options.DefaultPolicy = new AuthorizationPolicyBuilder()
//                                .AddAuthenticationSchemes("ApiKey")
//                                .RequireAuthenticatedUser()
//                                .Build();

//    options.AddPolicy("ApiKey", policy => policy
//                                .AddAuthenticationSchemes("ApiKey")
//                                .RequireAuthenticatedUser());
//});

builder.Services.AddTransient<IApiKeyValidator, CustomApiKeyValidator>();

// Uncomment the following line if you have multiple authentication schemes and
// you need to determine the authentication scheme at runtime (for example, you don't want to use the default authentication scheme).
//builder.Services.AddSingleton<IAuthenticationSchemeProvider, ApplicationAuthenticationSchemeProvider>();

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    //options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    //{
    //    In = ParameterLocation.Header,
    //    Description = "Insert JWT token with the \"Bearer \" prefix",
    //    Name = "Authorization",
    //    Type = SecuritySchemeType.ApiKey
    //});

    //options.AddSecurityRequirement(new OpenApiSecurityRequirement
    //            {
    //                {
    //                    new OpenApiSecurityScheme
    //                    {
    //                        Reference = new OpenApiReference
    //                        {
    //                            Type = ReferenceType.SecurityScheme,
    //                            Id = "ApiKey" //JwtBearerDefaults.AuthenticationScheme
    //                        }
    //                    },
    //                    Array.Empty<string>()
    //                }
    //            });

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

app.MapGet("api/me", (ClaimsPrincipal user, HttpContext context) =>
{
    return new User(user.Identity!.Name);
})
.RequireAuthorization()
.WithOpenApiAuthentication(builder.Configuration);

app.Run();

public record class User(string? UserName);

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