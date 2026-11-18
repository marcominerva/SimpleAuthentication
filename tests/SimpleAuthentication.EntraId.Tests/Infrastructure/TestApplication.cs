using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace SimpleAuthentication.EntraId.Tests.Infrastructure;

internal static class TestApplication
{
    private const string MetadataHttpClientName = nameof(FakeEntraAuthority);

    /// <summary>
    /// Creates an in-memory application that runs in the Development environment, so that the service provider validates scopes.
    /// </summary>
    public static WebApplicationBuilder CreateBuilder(IEnumerable<KeyValuePair<string, string?>> configuration)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Development });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

        return builder;
    }

    /// <summary>
    /// Creates the settings of an application that is registered in the tenant of the fake authority.
    /// </summary>
    public static Dictionary<string, string?> CreateEntraIdConfiguration(this FakeEntraAuthority authority, string clientId, string sectionName = "AzureAd")
        => new()
        {
            [$"{sectionName}:Instance"] = FakeEntraAuthority.Instance,
            [$"{sectionName}:TenantId"] = authority.TenantId,
            [$"{sectionName}:ClientId"] = clientId,
            [$"{sectionName}:ClientCredentials:0:SourceType"] = "ClientSecret",
            [$"{sectionName}:ClientCredentials:0:ClientSecret"] = Guid.NewGuid().ToString("N")
        };

    /// <summary>
    /// Redirects to the fake authority the requests that the Microsoft Entra ID scheme and MSAL send to Microsoft Entra ID.
    /// Everything else (token validation, token acquisition and caching) is the real implementation.
    /// </summary>
    public static IServiceCollection AddFakeEntraAuthority(this IServiceCollection services, FakeEntraAuthority authority, string schemeName = EntraIdDefaults.AuthenticationScheme)
    {
        services.AddHttpClient(MetadataHttpClientName).ConfigurePrimaryHttpMessageHandler(authority.CreateHandler);
        services.Configure<AadIssuerValidatorOptions>(options => options.HttpClientName = MetadataHttpClientName);
        services.Configure<JwtBearerOptions>(schemeName, options => options.BackchannelHttpHandler = authority.CreateHandler());

        services.RemoveAll<IMsalHttpClientFactory>();
        services.AddSingleton<IMsalHttpClientFactory>(new MsalHttpClientFactory(authority));

        return services;
    }

    private sealed class MsalHttpClientFactory(FakeEntraAuthority authority) : IMsalHttpClientFactory
    {
        private readonly HttpClient httpClient = new(authority.CreateHandler());

        public HttpClient GetHttpClient() => httpClient;
    }
}
