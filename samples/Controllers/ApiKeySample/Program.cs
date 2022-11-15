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
//                                .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
//                                .RequireAuthenticatedUser()
//                                .Build();

//    options.AddPolicy("ApiKey", policy => policy
//                                .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
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