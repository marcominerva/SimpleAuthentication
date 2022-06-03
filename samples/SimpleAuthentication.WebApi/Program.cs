using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using SimpleAuthentication;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.WebApi.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddSimpleAuthentication(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy = new AuthorizationPolicyBuilder()
                                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                                .RequireAuthenticatedUser()
                                .Build();

    options.AddPolicy("ApiKey", policy => policy
                                .AddAuthenticationSchemes("ApiKey")
                                .RequireAuthenticatedUser());
});

builder.Services.AddTransient<IApiKeyValidator, CustomApiKeyValidator>();

builder.Services.AddSingleton<IAuthenticationSchemeProvider, ApplicationAuthenticationSchemeProvider>();
builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformer>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSimpleAuthentication(builder.Configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthenticationAndAuthorization();

app.MapControllers();

app.Run();

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