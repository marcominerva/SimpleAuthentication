using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SimpleAuthentication.EntraId.Tests.Infrastructure;
using SimpleAuthentication.JwtBearer;

namespace SimpleAuthentication.EntraId.Tests;

public class EntraIdAuthenticationTests
{
    private const string LocalSchemeName = "Bearer";
    private const string RequiredRole = "Orders.Read";

    [Fact]
    public async Task AppOnlyTokenWithRequiredRole_IsAuthorizedAsync()
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", api.CreateToken());

        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task Version1TokenWithRequiredRole_IsAuthorizedAsync()
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", api.CreateVersion1Token());

        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task ConfiguredAudience_ReplacesDefaultOneAsync()
    {
        const string audience = "api://orders.example.com";
        await using var api = await TestApi.StartAsync(configuration => configuration["AzureAd:Audience"] = audience);

        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra", api.CreateToken(audience: audience)));
        Assert.Equal(HttpStatusCode.Unauthorized, await api.GetAsync("/entra", api.CreateToken()));
    }

    [Fact]
    public async Task MissingToken_IsUnauthorizedAsync()
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
    }

    [Theory]
    [InlineData("WrongAudience")]
    [InlineData("OtherTenant")]
    [InlineData("UntrustedIssuer")]
    [InlineData("Expired")]
    [InlineData("NotYetValid")]
    [InlineData("WrongSignature")]
    [InlineData("Unsigned")]
    [InlineData("WithoutExpiration")]
    public async Task InvalidToken_IsUnauthorizedAsync(string kind)
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", api.CreateInvalidToken(kind));

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
    }

    [Fact]
    public async Task TokenOfAnotherTenant_IsUnauthorized_EvenIfConfigurationAcceptsIssuersOfAnyTenantAsync()
    {
        await using var api = await TestApi.StartAsync(configuration =>
            configuration["AzureAd:TokenValidationParameters:ValidIssuers:0"] = "https://login.microsoftonline.com/{tenantid}/v2.0");

        var statusCode = await api.GetAsync("/entra", api.CreateInvalidToken("OtherTenant"));

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra", api.CreateToken()));
    }

    [Theory]
    [InlineData("WrongAudience")]
    [InlineData("OtherTenant")]
    [InlineData("UntrustedIssuer")]
    [InlineData("Expired")]
    [InlineData("Unsigned")]
    [InlineData("WithoutExpiration")]
    public async Task InvalidToken_IsUnauthorized_EvenIfConfigurationDisablesValidationAsync(string kind)
    {
        // The issuer of the signing keys is not published, so that the validations enforced by the scheme are the only ones that can reject the token.
        await using var api = await TestApi.StartAsync(configuration =>
        {
            configuration["AzureAd:TokenValidationParameters:RequireSignedTokens"] = "false";
            configuration["AzureAd:TokenValidationParameters:RequireExpirationTime"] = "false";
            configuration["AzureAd:TokenValidationParameters:ValidateIssuer"] = "false";
            configuration["AzureAd:TokenValidationParameters:ValidateAudience"] = "false";
            configuration["AzureAd:TokenValidationParameters:ValidateLifetime"] = "false";
        },
        signingKeyIssuerIsPublished: false);

        var statusCode = await api.GetAsync("/entra", api.CreateInvalidToken(kind));

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra", api.CreateToken()));
    }

    [Fact]
    public async Task AppOnlyTokenWithoutRequiredRole_IsForbiddenAsync()
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", api.CreateToken(roles: ["Orders.Write"]));

        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Theory]
    [InlineData(null, "Orders.Read")]
    [InlineData("user", null)]
    [InlineData("user", "Orders.Read")]
    [InlineData("app", "Orders.Read")]
    [InlineData(null, null)]
    public async Task TokenThatIsNotAppOnly_IsForbidden_EvenIfItContainsRequiredRoleAsync(string? identityType, string? scope)
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", api.CreateToken(identityType: identityType, scope: scope));

        Assert.Equal(HttpStatusCode.Forbidden, statusCode);
    }

    [Theory]
    [InlineData("Orders.Write", HttpStatusCode.OK)]
    [InlineData("Orders.Admin", HttpStatusCode.OK)]
    [InlineData("Orders.Read", HttpStatusCode.Forbidden)]
    public async Task NamedPolicy_RequiresOneOfItsRolesAsync(string role, HttpStatusCode expectedStatusCode)
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra/policy", api.CreateToken(roles: [role]));

        Assert.Equal(expectedStatusCode, statusCode);
    }

    [Fact]
    public async Task Registration_DoesNotChangeDefaultSchemesAndPoliciesAsync()
    {
        await using var api = await TestApi.StartAsync();

        // The local scheme was the only one, so ASP.NET Core used it as the default scheme: it must remain the default one.
        var authenticationOptions = api.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        Assert.Equal(LocalSchemeName, authenticationOptions.DefaultScheme);
        Assert.Equal(LocalSchemeName, authenticationOptions.DefaultAuthenticateScheme);
        Assert.Equal(LocalSchemeName, authenticationOptions.DefaultChallengeScheme);

        var authorizationOptions = api.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.Null(authorizationOptions.FallbackPolicy);
        Assert.Empty(authorizationOptions.DefaultPolicy.AuthenticationSchemes);
        Assert.Contains(authorizationOptions.DefaultPolicy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task EndpointProtectedByDefaultPolicy_KeepsUsingSchemeSelectedAutomaticallyAsync()
    {
        // Without a default scheme, ASP.NET Core automatically uses the only scheme registered by the application.
        await using var api = await TestApi.StartAsync(configuration => configuration.Remove("Authentication:DefaultScheme"));

        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/local", await api.CreateLocalTokenAsync()));
        Assert.Equal(HttpStatusCode.Unauthorized, await api.GetAsync("/local", api.CreateToken()));
        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra", api.CreateToken()));
    }

    [Theory]
    [InlineData(0, null, null)]
    [InlineData(1, null, LocalSchemeName)]
    [InlineData(2, null, null)]
    [InlineData(1, EntraIdDefaults.AuthenticationScheme, EntraIdDefaults.AuthenticationScheme)]
    public async Task DefaultScheme_IsSetOnlyToPreserveSchemeSelectedAutomaticallyAsync(int schemesOfHost, string? defaultSchemeOfHost, string? expectedDefaultScheme)
    {
        await using var api = await TestApi.StartAsync(configuration =>
        {
            configuration.Remove("Authentication:DefaultScheme");

            if (schemesOfHost == 0)
            {
                foreach (var key in configuration.Keys.Where(key => key.StartsWith("Authentication:JwtBearer:", StringComparison.Ordinal)).ToList())
                {
                    configuration.Remove(key);
                }
            }
            else if (schemesOfHost == 2)
            {
                configuration["Authentication:ApiKey:HeaderName"] = "x-api-key";
                configuration["Authentication:ApiKey:ApiKeyValue"] = Guid.NewGuid().ToString("N");
                configuration["Authentication:ApiKey:UserName"] = "local-service";
            }
        },
        configureServices: services => services.Configure<AuthenticationOptions>(options => options.DefaultScheme ??= defaultSchemeOfHost));

        var authenticationOptions = api.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        Assert.Equal(schemesOfHost + 1, authenticationOptions.Schemes.Count());
        Assert.Equal(expectedDefaultScheme, authenticationOptions.DefaultScheme);
        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra", api.CreateToken()));
    }

    [Fact]
    public async Task EndpointWithoutAuthorization_RemainsAnonymousAsync()
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/anonymous", token: null);

        Assert.Equal(HttpStatusCode.OK, statusCode);
    }

    [Fact]
    public async Task EndpointProtectedByDefaultPolicy_KeepsUsingDefaultSchemeAsync()
    {
        await using var api = await TestApi.StartAsync();

        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/local", await api.CreateLocalTokenAsync()));
        Assert.Equal(HttpStatusCode.Unauthorized, await api.GetAsync("/local", api.CreateToken()));
        Assert.Equal(HttpStatusCode.Unauthorized, await api.GetAsync("/local", token: null));
    }

    [Fact]
    public async Task TokenOfAnotherScheme_IsUnauthorized_EvenIfItContainsApplicationRoleClaimsAsync()
    {
        await using var api = await TestApi.StartAsync();

        var statusCode = await api.GetAsync("/entra", await api.CreateLocalTokenAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
    }

    [Fact]
    public async Task PolicyThatAcceptsMultipleSchemes_EvaluatesRolesOfEntraIdIdentityOnlyAsync()
    {
        await using var api = await TestApi.StartAsync();

        Assert.Equal(HttpStatusCode.Forbidden, await api.GetAsync("/entra/mixed", await api.CreateLocalTokenAsync()));
        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra/mixed", api.CreateToken()));
    }

    [Fact]
    public async Task CustomSchemeName_IsUsedByPoliciesThatReferenceItAsync()
    {
        await using var api = await TestApi.StartAsync(schemeName: "Partners");

        Assert.Equal(HttpStatusCode.OK, await api.GetAsync("/entra/policy", api.CreateToken(roles: ["Orders.Write"])));
        Assert.Equal(HttpStatusCode.Forbidden, await api.GetAsync("/entra/policy", api.CreateToken()));
        Assert.Equal(HttpStatusCode.Unauthorized, await api.GetAsync("/entra/policy", await api.CreateLocalTokenAsync()));
    }

    [Theory]
    [InlineData("TenantId", null)]
    [InlineData("TenantId", "common")]
    [InlineData("TenantId", "organizations")]
    [InlineData("TenantId", "contoso.onmicrosoft.com")]
    [InlineData("TenantId", "{11111111-1111-1111-1111-111111111111}")]
    [InlineData("ClientId", null)]
    [InlineData("ClientId", "orders-api")]
    public void InvalidIdentifier_IsRejectedAtRegistration(string key, string? value)
    {
        using var authority = new FakeEntraAuthority();
        var settings = authority.CreateEntraIdConfiguration(Guid.NewGuid().ToString());
        settings[$"AzureAd:{key}"] = value;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var builder = new ServiceCollection().AddAuthentication();

        var exception = Assert.Throws<ArgumentException>(() => builder.AddEntraIdJwtBearer(configuration.GetSection("AzureAd")));

        Assert.Equal(key, exception.ParamName);
        Assert.Contains($"AzureAd:{key}", exception.Message);
    }

    [Fact]
    public void InvalidArguments_AreRejectedAtRegistration()
    {
        var configuration = new ConfigurationBuilder().Build();
        var builder = new ServiceCollection().AddAuthentication();

        Assert.Throws<ArgumentNullException>(() => EntraIdAuthenticationExtensions.AddEntraIdJwtBearer(null!, configuration.GetSection("AzureAd")));
        Assert.Throws<ArgumentNullException>(() => builder.AddEntraIdJwtBearer(null!));
        Assert.Throws<ArgumentException>(() => builder.AddEntraIdJwtBearer(configuration.GetSection("AzureAd"), " "));
    }

    [Fact]
    public async Task IncompleteConfiguration_IsReportedWhenApplicationStartsAsync()
    {
        var exception = await Assert.ThrowsAnyAsync<Exception>(() => TestApi.StartAsync(configuration => configuration.Remove("AzureAd:Instance")).AsTask());

        Assert.Contains("Instance", exception.Message);
    }

    private sealed class TestApi(FakeEntraAuthority authority, string clientId, WebApplication application) : IAsyncDisposable
    {
        private readonly HttpClient client = application.GetTestClient();

        public IServiceProvider Services => application.Services;

        public static async ValueTask<TestApi> StartAsync(Action<Dictionary<string, string?>>? configure = null, string schemeName = EntraIdDefaults.AuthenticationScheme,
            bool signingKeyIssuerIsPublished = true, Action<IServiceCollection>? configureServices = null)
        {
            var authority = new FakeEntraAuthority(signingKeyIssuerIsPublished);
            var clientId = Guid.NewGuid().ToString();

            var configuration = authority.CreateEntraIdConfiguration(clientId);
            configuration["Authentication:DefaultScheme"] = LocalSchemeName;
            configuration["Authentication:JwtBearer:SecurityKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            configuration["Authentication:JwtBearer:Issuers:0"] = "local-issuer";
            configuration["Authentication:JwtBearer:Audiences:0"] = "local-audience";
            configure?.Invoke(configuration);

            var builder = TestApplication.CreateBuilder(configuration);

            builder.Services.AddSimpleAuthentication(builder.Configuration)
                .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"), schemeName);

            builder.Services.AddFakeEntraAuthority(authority, schemeName);
            configureServices?.Invoke(builder.Services);

            builder.Services.AddAuthorizationBuilder()
                .AddPolicy("OrdersWrite", policy => policy.RequireEntraIdApplicationRole(["Orders.Write", "Orders.Admin"], schemeName));

            var application = builder.Build();

            application.UseAuthentication();
            application.UseAuthorization();

            application.MapGet("/anonymous", () => Results.Ok());
            application.MapGet("/local", () => Results.Ok()).RequireAuthorization();
            application.MapGet("/entra/policy", () => Results.Ok()).RequireAuthorization("OrdersWrite");

            if (schemeName == EntraIdDefaults.AuthenticationScheme)
            {
                application.MapGet("/entra", () => Results.Ok()).RequireEntraIdApplicationRole(RequiredRole);
                application.MapGet("/entra/mixed", () => Results.Ok())
                    .RequireAuthorization(policy => policy.AddAuthenticationSchemes(LocalSchemeName).RequireEntraIdApplicationRole(RequiredRole));
            }

            try
            {
                await application.StartAsync(TestContext.Current.CancellationToken);
            }
            catch
            {
                await application.DisposeAsync();
                authority.Dispose();
                throw;
            }

            return new(authority, clientId, application);
        }

        public string CreateToken(IEnumerable<string>? roles = null, string? identityType = "app", string? scope = null, string? audience = null)
            => authority.CreateToken(audience ?? clientId, roles ?? [RequiredRole], identityType, scope);

        /// <summary>
        /// Creates a version 1.0 access token, that uses a different issuer and the Application ID URI of the API as audience.
        /// </summary>
        public string CreateVersion1Token()
            => authority.CreateToken($"api://{clientId}", [RequiredRole], version1: true);

        public string CreateInvalidToken(string kind)
        {
            var now = DateTime.UtcNow;

            return kind switch
            {
                "WrongAudience" => authority.CreateToken(Guid.NewGuid().ToString(), [RequiredRole]),
                "OtherTenant" => authority.CreateToken(clientId, [RequiredRole], tenantId: Guid.NewGuid().ToString()),
                "UntrustedIssuer" => authority.CreateToken(clientId, [RequiredRole], issuer: "https://login.example.com/v2.0"),
                "Expired" => authority.CreateToken(clientId, [RequiredRole], notBefore: now.AddHours(-2), expires: now.AddHours(-1)),
                "NotYetValid" => authority.CreateToken(clientId, [RequiredRole], notBefore: now.AddHours(1), expires: now.AddHours(2)),
                "WrongSignature" => CreateTokenSignedByAnotherKey(),
                "Unsigned" => authority.CreateToken(clientId, [RequiredRole], signed: false),
                "WithoutExpiration" => authority.CreateToken(clientId, [RequiredRole], hasExpiration: false),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        /// <summary>
        /// Creates a token for the default scheme of the application that pretends to be an app-only Microsoft Entra ID token with the required role.
        /// </summary>
        public Task<string> CreateLocalTokenAsync()
        {
            var claims = new Claim[]
            {
                new("roles", RequiredRole),
                new(ClaimTypes.Role, RequiredRole),
                new("idtyp", "app"),
                new("tid", authority.TenantId)
            };

            return Services.GetRequiredService<IJwtBearerService>().CreateTokenAsync("local-user", claims, "local-issuer", "local-audience");
        }

        public async Task<HttpStatusCode> GetAsync(string path, string? token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
            return response.StatusCode;
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await application.DisposeAsync();
            authority.Dispose();
        }

        private string CreateTokenSignedByAnotherKey()
        {
            using var rsa = RSA.Create(2048);
            var securityKey = new RsaSecurityKey(rsa) { KeyId = authority.SigningCredentials.Key.KeyId };

            return authority.CreateToken(clientId, [RequiredRole], signingCredentials: new(securityKey, SecurityAlgorithms.RsaSha256));
        }
    }
}
