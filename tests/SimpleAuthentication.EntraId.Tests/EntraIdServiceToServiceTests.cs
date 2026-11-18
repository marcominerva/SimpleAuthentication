using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using SimpleAuthentication.EntraId.Tests.Infrastructure;

namespace SimpleAuthentication.EntraId.Tests;

/// <summary>
/// Exercises the whole flow between two APIs using the real Microsoft.Identity.Web and MSAL implementations: only the endpoints
/// of Microsoft Entra ID are simulated, by <see cref="FakeEntraAuthority"/>.
/// </summary>
public class EntraIdServiceToServiceTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OrdersApi_CallsInventoryApi_UsingItsOwnAppOnlyTokenAsync()
    {
        await using var services = await Services.StartAsync("Inventory.Read");
        var callerToken = services.CreateOrdersApiToken();

        var firstResponse = await services.GetOrdersAsync(callerToken);
        var secondResponse = await services.GetOrdersAsync(callerToken);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        // The Inventory API has been called by the Orders API itself, not by the application that called the Orders API.
        Assert.Equal(services.OrdersClientId, firstResponse.InventoryCaller);
        Assert.Equal(services.OrdersClientId, secondResponse.InventoryCaller);

        // The second call uses the token cache registered by the host.
        var tokenRequest = Assert.Single(services.Authority.TokenRequests);
        Assert.Equal("client_credentials", tokenRequest["grant_type"]);
        Assert.Equal(services.OrdersClientId, tokenRequest["client_id"]);
        Assert.Equal($"api://{services.InventoryClientId}/.default", tokenRequest["scope"]);
        Assert.DoesNotContain("assertion", tokenRequest.Keys);
        Assert.DoesNotContain(callerToken, tokenRequest.Values);
    }

    [Theory]
    [InlineData("Inventory.Write", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task InventoryApi_RejectsOrdersApi_WhenRequiredApplicationRoleHasNotBeenGrantedAsync(string? grantedRole, HttpStatusCode expectedStatusCode)
    {
        await using var services = await Services.StartAsync(grantedRole);

        var response = await services.GetOrdersAsync(services.CreateOrdersApiToken());

        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    [Fact]
    public async Task InventoryApi_CanBeCalled_OutsideOfIncomingRequestAsync()
    {
        await using var services = await Services.StartAsync("Inventory.Read");
        using var client = services.OrdersApi.Services.GetRequiredService<IHttpClientFactory>().CreateClient("Inventory");

        var caller = await client.GetFromJsonAsync<string>("products/caller", CancellationToken);

        Assert.Equal(services.OrdersClientId, caller);
    }

    private sealed record OrdersResponse(HttpStatusCode StatusCode, string? InventoryCaller);

    private sealed class Services(FakeEntraAuthority authority, string ordersClientId, string inventoryClientId, WebApplication ordersApi, WebApplication inventoryApi) : IAsyncDisposable
    {
        private readonly HttpClient client = ordersApi.GetTestClient();

        public FakeEntraAuthority Authority => authority;

        public string OrdersClientId => ordersClientId;

        public string InventoryClientId => inventoryClientId;

        public WebApplication OrdersApi => ordersApi;

        public static async ValueTask<Services> StartAsync(string? roleGrantedToOrdersApi)
        {
            var authority = new FakeEntraAuthority();
            var ordersClientId = Guid.NewGuid().ToString();
            var inventoryClientId = Guid.NewGuid().ToString();

            if (roleGrantedToOrdersApi is not null)
            {
                authority.GrantApplicationRoles(inventoryClientId, roleGrantedToOrdersApi);
            }

            var inventoryApi = await StartInventoryApiAsync(authority, inventoryClientId);
            var ordersApi = await StartOrdersApiAsync(authority, ordersClientId, inventoryClientId, inventoryApi.GetTestServer());

            return new(authority, ordersClientId, inventoryClientId, ordersApi, inventoryApi);
        }

        public string CreateOrdersApiToken() => authority.CreateToken(ordersClientId, ["Orders.Read"]);

        public async Task<OrdersResponse> GetOrdersAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request, CancellationToken);
            var inventoryCaller = response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<string>(CancellationToken) : null;

            return new(response.StatusCode, inventoryCaller);
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await ordersApi.DisposeAsync();
            await inventoryApi.DisposeAsync();
            authority.Dispose();
        }

        private static async Task<WebApplication> StartInventoryApiAsync(FakeEntraAuthority authority, string clientId)
        {
            var builder = TestApplication.CreateBuilder(authority.CreateEntraIdConfiguration(clientId));

            builder.Services.AddAuthentication()
                .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddFakeEntraAuthority(authority);

            var application = builder.Build();

            // Returns the identifier of the application that has called the API.
            application.MapGet("/products/caller", (ClaimsPrincipal user) => Results.Json(user.FindFirstValue("azp")))
                .RequireEntraIdApplicationRole("Inventory.Read");

            await application.StartAsync(CancellationToken);
            return application;
        }

        private static async Task<WebApplication> StartOrdersApiAsync(FakeEntraAuthority authority, string clientId, string inventoryClientId, TestServer inventoryApi)
        {
            var configuration = authority.CreateEntraIdConfiguration(clientId);
            configuration["DownstreamApis:Inventory:BaseAddress"] = "https://inventory.example.com/";
            configuration["DownstreamApis:Inventory:Scope"] = $"api://{inventoryClientId}/.default";

            var builder = TestApplication.CreateBuilder(configuration);

            builder.Services.AddAuthentication()
                .AddEntraIdJwtBearer(builder.Configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
                .AddInMemoryTokenCaches();

            builder.Services.AddFakeEntraAuthority(authority);

            builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"))
                .ConfigurePrimaryHttpMessageHandler(inventoryApi.CreateHandler);

            var application = builder.Build();

            application.MapGet("/orders", async (IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
            {
                using var inventoryClient = httpClientFactory.CreateClient("Inventory");
                using var response = await inventoryClient.GetAsync("products/caller", cancellationToken);

                return response.IsSuccessStatusCode
                    ? Results.Json(await response.Content.ReadFromJsonAsync<string>(cancellationToken))
                    : Results.StatusCode((int)response.StatusCode);
            })
            .RequireEntraIdApplicationRole("Orders.Read");

            await application.StartAsync(CancellationToken);
            return application;
        }
    }
}
