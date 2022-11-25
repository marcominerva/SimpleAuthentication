using JwtBearerSample.Authentication;
using Microsoft.AspNetCore.Authentication;
using SimpleAuthentication;

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

app.MapControllers();

app.Run();