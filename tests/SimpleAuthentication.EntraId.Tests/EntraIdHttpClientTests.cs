using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using NSubstitute;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using SimpleAuthentication.EntraId.Tests.Infrastructure;

namespace SimpleAuthentication.EntraId.Tests;

public class EntraIdHttpClientTests
{
    private const string ClientName = "Inventory";
    private const string BaseAddress = "https://inventory.example.com/api/";
    private const string Scope = "api://inventory/.default";
    private const string AccessToken = "app-only-access-token";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Request_IsSentWithAppOnlyTokenAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));

        using var response = await fixture.Client.GetAsync("products?page=2", CancellationToken);

        var request = Assert.Single(fixture.PrimaryHandler.Requests);
        Assert.Equal("https://inventory.example.com/api/products?page=2", request.RequestUri.AbsoluteUri);
        Assert.Equal($"Bearer {AccessToken}", request.Authorization);

        await fixture.AuthorizationHeaderProvider.Received(1).CreateAuthorizationHeaderForAppAsync(Scope,
            Arg.Is<AuthorizationHeaderProviderOptions>(options => options.AcquireTokenOptions.AuthenticationOptionsName == EntraIdDefaults.AuthenticationScheme), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_IsAcquiredUsingConfiguredAuthenticationSchemeAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK), settings => settings[$"{ClientName}:AuthenticationScheme"] = "Partners");

        using var response = await fixture.Client.GetAsync("products", CancellationToken);

        await fixture.AuthorizationHeaderProvider.Received(1).CreateAuthorizationHeaderForAppAsync(Scope,
            Arg.Is<AuthorizationHeaderProviderOptions>(options => options.AcquireTokenOptions.AuthenticationOptionsName == "Partners"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Token_IsRemovedFromRequest_WhenResponseIsReceivedAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));
        using var request = new HttpRequestMessage(HttpMethod.Get, "products");

        using var response = await fixture.Client.SendAsync(request, CancellationToken);

        Assert.Equal($"Bearer {AccessToken}", Assert.Single(fixture.PrimaryHandler.Requests).Authorization);
        Assert.Null(request.Headers.Authorization);
    }

    [Theory]
    [InlineData("products", "https://inventory.example.com/api/products")]
    [InlineData("products/42?expand=stock", "https://inventory.example.com/api/products/42?expand=stock")]
    [InlineData("products/a%2Fb", "https://inventory.example.com/api/products/a%2Fb")]
    [InlineData("products;version=2", "https://inventory.example.com/api/products;version=2")]
    [InlineData("", "https://inventory.example.com/api/")]
    [InlineData("https://INVENTORY.example.com:443/api/products", "https://inventory.example.com/api/products")]
    public async Task RequestInsideBaseAddress_IsSentAsync(string requestUri, string expectedUri)
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));

        using var response = await fixture.Client.GetAsync(requestUri, CancellationToken);

        Assert.Equal(expectedUri, Assert.Single(fixture.PrimaryHandler.Requests).RequestUri.AbsoluteUri);
    }

    [Theory]
    [InlineData("https://other.example.com/api/products")]
    [InlineData("https://inventory.example.com.other.example/api/products")]
    [InlineData("http://inventory.example.com/api/products")]
    [InlineData("https://inventory.example.com:8443/api/products")]
    [InlineData("https://user@inventory.example.com/api/products")]
    [InlineData("//other.example.com/api/products")]
    [InlineData("https://inventory.example.com/api")]
    [InlineData("https://inventory.example.com/API/products")]
    [InlineData("/products")]
    [InlineData("../products")]
    [InlineData("%2e%2e/products")]
    [InlineData("..%2Fproducts")]
    [InlineData("..%5Cproducts")]
    [InlineData("%252e%252e%252Fproducts")]
    [InlineData("..;/products")]
    [InlineData("%2e%2e;/products")]
    [InlineData("products/..;/..;/admin")]
    public async Task RequestOutsideBaseAddress_IsRejectedBeforeAcquiringTokenAsync(string requestUri)
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Client.GetAsync(requestUri, CancellationToken));

        Assert.Empty(fixture.PrimaryHandler.Requests);
        Assert.Empty(fixture.AuthorizationHeaderProvider.ReceivedCalls());
    }

    [Theory]
    [InlineData("RequestHeader")]
    [InlineData("RequestHeaderWithoutValidation")]
    [InlineData("DefaultRequestHeader")]
    [InlineData("HostHeader")]
    [InlineData("ClientBaseAddress")]
    public async Task RequestThatOverridesDestinationOrCredentials_IsRejectedBeforeAcquiringTokenAsync(string kind)
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));
        using var request = new HttpRequestMessage(HttpMethod.Get, "products");

        switch (kind)
        {
            case "RequestHeader":
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token-of-the-incoming-request");
                break;

            case "RequestHeaderWithoutValidation":
                request.Headers.TryAddWithoutValidation("Authorization", "token-of-the-incoming-request");
                break;

            case "DefaultRequestHeader":
                fixture.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-of-the-incoming-request");
                break;

            case "HostHeader":
                request.Headers.Host = "other.example.com";
                break;

            case "ClientBaseAddress":
                fixture.Client.BaseAddress = new Uri("https://other.example.com/api/");
                break;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Client.SendAsync(request, CancellationToken));

        Assert.Empty(fixture.PrimaryHandler.Requests);
        Assert.Empty(fixture.AuthorizationHeaderProvider.ReceivedCalls());
    }

    [Fact]
    public async Task SynchronousRequest_IsNotSupportedAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));
        using var request = new HttpRequestMessage(HttpMethod.Get, "products");

        var exception = Assert.Throws<NotSupportedException>(() => fixture.Client.Send(request, CancellationToken));

        Assert.Contains(ClientName, exception.Message);
        Assert.Empty(fixture.PrimaryHandler.Requests);
    }

    [Fact]
    public async Task RequestCreatedByAnotherHandler_DoesNotReceiveTokenAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK),
            configureServices: services => services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler(() => new ReplacingRequestHandler())));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Client.GetAsync("products", CancellationToken));

        Assert.Empty(fixture.PrimaryHandler.Requests);
        Assert.Empty(fixture.AuthorizationHeaderProvider.ReceivedCalls());
    }

    [Fact]
    public async Task RequestRoutedByAnotherHandler_IsValidatedUsingAddressChosenByCallerAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK),
            configureServices: services => services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler(() => new EndpointResolvingHandler())));

        using var response = await fixture.Client.GetAsync("products", CancellationToken);

        var request = Assert.Single(fixture.PrimaryHandler.Requests);
        Assert.Equal("https://10.0.0.4:8443/api/products", request.RequestUri.AbsoluteUri);
        Assert.Equal($"Bearer {AccessToken}", request.Authorization);
    }

    [Fact]
    public async Task SafeRequest_IsRetried_UsingCurrentTokenForEveryAttemptAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway, HttpStatusCode.OK));

        using var response = await fixture.Client.GetAsync("products", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, fixture.PrimaryHandler.Requests.Count);
        Assert.All(fixture.PrimaryHandler.Requests, request => Assert.Equal($"Bearer {AccessToken}", request.Authorization));
        Assert.Equal(3, fixture.AuthorizationHeaderProvider.ReceivedCalls().Count());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    public async Task SafeRequest_IsRetried_WhenConnectionFailsAsync(string method)
    {
        await using var fixture = Fixture.Create(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("The connection has been reset.")),
            configureResilience: options => options.Retry.MaxRetryAttempts = 2);
        using var request = new HttpRequestMessage(new HttpMethod(method), "products");

        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Client.SendAsync(request, CancellationToken));

        Assert.Equal(3, fixture.PrimaryHandler.Requests.Count);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("MERGE")]
    public async Task RequestThatCanWriteData_IsNotRetried_WhenResponseIsTransientErrorAsync(string method)
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable));
        using var request = new HttpRequestMessage(new HttpMethod(method), "products") { Content = new StringContent("{}") };

        using var response = await fixture.Client.SendAsync(request, CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Single(fixture.PrimaryHandler.Requests);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task RequestThatCanWriteData_IsNotRetried_WhenConnectionFailsAsync(string method)
    {
        await using var fixture = Fixture.Create(new StubHttpMessageHandler((_, _) => throw new HttpRequestException("The connection has been reset.")));
        using var request = new HttpRequestMessage(new HttpMethod(method), "products") { Content = new StringContent("{}") };

        await Assert.ThrowsAsync<HttpRequestException>(() => fixture.Client.SendAsync(request, CancellationToken));

        Assert.Single(fixture.PrimaryHandler.Requests);
    }

    [Theory]
    [InlineData("GET", 3)]
    [InlineData("POST", 1)]
    public async Task AttemptTimeout_IsApplied_AndRetriesOnlySafeRequestsAsync(string method, int expectedRequests)
    {
        await using var fixture = Fixture.Create(new StubHttpMessageHandler(async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }),
            configureResilience: options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(100);
                options.Retry.MaxRetryAttempts = 2;
            });
        using var request = new HttpRequestMessage(new HttpMethod(method), "products");

        await Assert.ThrowsAsync<TimeoutRejectedException>(() => fixture.Client.SendAsync(request, CancellationToken));

        Assert.Equal(expectedRequests, fixture.PrimaryHandler.Requests.Count);
    }

    [Fact]
    public async Task RequestMarkedAsIdempotent_IsRetriedAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK));
        using var request = new HttpRequestMessage(HttpMethod.Put, "products/42") { Content = new StringContent("{}") };

        using var response = await fixture.Client.SendAsync(request.MarkAsIdempotent(), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, fixture.PrimaryHandler.Requests.Count);
    }

    [Fact]
    public async Task ResilienceConfiguration_CannotEnableRetriesOfRequestsThatCanWriteDataAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.NotFound), configureResilience: options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.ShouldHandle = _ => ValueTask.FromResult(true);
        });

        using var postResponse = await fixture.Client.PostAsync("products", new StringContent("{}"), CancellationToken);
        Assert.Single(fixture.PrimaryHandler.Requests);

        using var getResponse = await fixture.Client.GetAsync("products", CancellationToken);
        Assert.Equal(4, fixture.PrimaryHandler.Requests.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResilienceHandlerOfHttpClientDefaults_IsNotStackedAsync(bool defaultsAreRegisteredFirst)
    {
        static void ConfigureDefaults(IServiceCollection services)
            => services.ConfigureHttpClientDefaults(builder => builder.AddStandardResilienceHandler(ConfigureFastRetries));

        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable),
            configureResilience: options => options.Retry.MaxRetryAttempts = 2,
            configureServices: defaultsAreRegisteredFirst ? ConfigureDefaults : null,
            configureClient: defaultsAreRegisteredFirst ? null : builder => ConfigureDefaults(builder.Services));

        using var response = await fixture.Client.GetAsync("products", CancellationToken);

        Assert.Equal(3, fixture.PrimaryHandler.Requests.Count);
    }

    [Fact]
    public async Task ClientRegisteredTwice_UsesSinglePipelineAndSingleTokenAsync()
    {
        var settings = CreateSettings();

        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable),
            configureResilience: options => options.Retry.MaxRetryAttempts = 2,
            configureClient: builder => builder.Services.AddEntraIdHttpClient(ClientName, settings, options =>
            {
                ConfigureFastRetries(options);
                options.Retry.MaxRetryAttempts = 2;
            }));

        using var response = await fixture.Client.GetAsync("products", CancellationToken);

        Assert.Equal(3, fixture.PrimaryHandler.Requests.Count);
        Assert.Equal(3, fixture.AuthorizationHeaderProvider.ReceivedCalls().Count());
    }

    [Fact]
    public async Task Resilience_IsConfiguredFromConfigurationSection_ThenFromCodeAsync()
    {
        static void ConfigureSettings(Dictionary<string, string?> settings) => settings[$"{ClientName}:Resilience:Retry:MaxRetryAttempts"] = "1";

        await using var configuredFromSection = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable), ConfigureSettings);
        using var firstResponse = await configuredFromSection.Client.GetAsync("products", CancellationToken);
        Assert.Equal(2, configuredFromSection.PrimaryHandler.Requests.Count);

        await using var configuredFromCode = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable), ConfigureSettings, options => options.Retry.MaxRetryAttempts = 2);
        using var secondResponse = await configuredFromCode.Client.GetAsync("products", CancellationToken);
        Assert.Equal(3, configuredFromCode.PrimaryHandler.Requests.Count);
    }

    [Fact]
    public async Task CircuitBreaker_StopsRequests_AfterRepeatedFailuresAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable), configureResilience: options =>
        {
            options.CircuitBreaker.MinimumThroughput = 2;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        });

        await Assert.ThrowsAnyAsync<BrokenCircuitException>(() => fixture.Client.GetAsync("products", CancellationToken));
        var requestsBeforeBreak = fixture.PrimaryHandler.Requests.Count;

        await Assert.ThrowsAnyAsync<BrokenCircuitException>(() => fixture.Client.GetAsync("products", CancellationToken));

        Assert.Equal(2, requestsBeforeBreak);
        Assert.Equal(requestsBeforeBreak, fixture.PrimaryHandler.Requests.Count);
    }

    [Fact(Timeout = 15_000)]
    public async Task Cancellation_IsPropagatedToTokenAcquisitionAsync()
    {
        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK));

        fixture.AuthorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(Arg.Any<string>(), Arg.Any<AuthorizationHeaderProviderOptions>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
                return $"Bearer {AccessToken}";
            });

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.GetAsync("products", cancellationTokenSource.Token));

        Assert.Single(fixture.AuthorizationHeaderProvider.ReceivedCalls());
        Assert.Empty(fixture.PrimaryHandler.Requests);
    }

    [Fact(Timeout = 15_000)]
    public async Task Cancellation_IsPropagatedToRequest_WithoutRetriesAsync()
    {
        await using var fixture = Fixture.Create(new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));

        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.GetAsync("products", cancellationTokenSource.Token));

        Assert.Single(fixture.PrimaryHandler.Requests);
    }

    [Fact(Timeout = 15_000)]
    public async Task CancellationRequestedByAnotherHandler_IsPropagatedToRequestAsync()
    {
        await using var fixture = Fixture.Create(new StubHttpMessageHandler(async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }),
            configureServices: services => services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler(() => new CancellingHandler(TimeSpan.FromMilliseconds(100)))));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Client.GetAsync("products", TestContext.Current.CancellationToken));

        Assert.Single(fixture.PrimaryHandler.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccessToken_IsNeverLoggedAsync(bool hostDisablesRedaction)
    {
        var loggerProvider = new CollectingLoggerProvider();

        await using var fixture = Fixture.Create(StubHttpMessageHandler.Returning(HttpStatusCode.OK),
            configureServices: services => services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider)),
            configureClient: builder =>
            {
                if (hostDisablesRedaction)
                {
                    builder.RedactLoggedHeaders(_ => false);
                }
            });

        using var response = await fixture.Client.GetAsync("products", CancellationToken);

        Assert.Contains(loggerProvider.Messages, message => message.Contains("Authorization: *"));
        Assert.DoesNotContain(loggerProvider.Messages, message => message.Contains(AccessToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(nameof(SocketsHttpHandler))]
    [InlineData(nameof(HttpClientHandler))]
    public async Task RealConnection_AllowsHttpOnLoopback_WithoutFollowingRedirectsOrUsingCookiesAsync(string? primaryHandlerChosenByHost)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        await using var api = builder.Build();
        var requestedPaths = new ConcurrentQueue<string>();

        api.Use((context, next) =>
        {
            requestedPaths.Enqueue(context.Request.Path);
            return next(context);
        });

        api.MapGet("/api/redirect", () => Results.Redirect("/api/authorization"));
        api.MapGet("/api/authorization", (HttpRequest request) => request.Headers.Authorization.ToString());
        api.MapGet("/api/cookie", (HttpContext context) =>
        {
            context.Response.Cookies.Append("session", "1");
            return context.Request.Headers.Cookie.ToString();
        });

        await api.StartAsync(CancellationToken);

        await using var fixture = Fixture.Create(primaryHandler: null, settings =>
        {
            settings[$"{ClientName}:BaseAddress"] = $"{api.Urls.Single()}/api/";
            settings[$"{ClientName}:AllowHttpOnLoopback"] = "true";
        },
        configureClient: builder =>
        {
            // The host can replace the primary handler after the registration of the client, for example to customize TLS settings.
            if (primaryHandlerChosenByHost is nameof(SocketsHttpHandler))
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler());
            }
            else if (primaryHandlerChosenByHost is nameof(HttpClientHandler))
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());
            }
        });

        using var redirectResponse = await fixture.Client.GetAsync("redirect", CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal("/api/redirect", Assert.Single(requestedPaths));

        Assert.Equal($"Bearer {AccessToken}", await fixture.Client.GetStringAsync("authorization", CancellationToken));

        await fixture.Client.GetStringAsync("cookie", CancellationToken);
        Assert.Empty(await fixture.Client.GetStringAsync("cookie", CancellationToken));
    }

    [Theory]
    [InlineData("Added")]
    [InlineData("Removed")]
    [InlineData("Replaced")]
    public void ResilienceHandlersChangedAfterRegistration_AreReportedWhenClientIsCreated(string change)
    {
        static void RemoveResilienceHandlers(IList<DelegatingHandler> handlers)
        {
            foreach (var handler in handlers.OfType<ResilienceHandler>().ToList())
            {
                handlers.Remove(handler);
            }
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Substitute.For<IAuthorizationHeaderProvider>());

        var builder = services.AddEntraIdHttpClient(ClientName, CreateSettings());

        switch (change)
        {
            case "Added":
                builder.AddStandardResilienceHandler();
                break;

            case "Removed":
                builder.ConfigureAdditionalHttpMessageHandlers((handlers, _) => RemoveResilienceHandlers(handlers));
                break;

            case "Replaced":
                builder.ConfigureAdditionalHttpMessageHandlers((handlers, _) => RemoveResilienceHandlers(handlers));
                builder.AddResilienceHandler("custom", pipeline => pipeline.AddRetry(new HttpRetryStrategyOptions()));
                break;
        }

        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var exception = Assert.Throws<InvalidOperationException>(() => httpClientFactory.CreateClient(ClientName));

        Assert.Contains(ClientName, exception.Message);
        Assert.Contains("resilience", exception.Message);
    }

    [Fact]
    public void MissingTokenAcquisitionServices_AreReportedWhenClientIsCreated()
    {
        var services = new ServiceCollection();
        services.AddEntraIdHttpClient(ClientName, CreateSettings());

        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var exception = Assert.Throws<InvalidOperationException>(() => httpClientFactory.CreateClient(ClientName));

        Assert.Contains("EnableTokenAcquisitionToCallDownstreamApi", exception.Message);
    }

    [Theory]
    [InlineData(null, Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("inventory", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("ftp://inventory.example.com/", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("http://inventory.example.com/", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("http://inventory.example.com/", Scope, null, true, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("http://localhost.example.com/", Scope, null, true, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("http://localhost:5080/", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("https://inventory.example.com/api", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("https://inventory.example.com/?version=1", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("https://inventory.example.com/#fragment", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData("https://user@inventory.example.com/", Scope, null, false, nameof(EntraIdHttpClientSettings.BaseAddress))]
    [InlineData(BaseAddress, null, null, false, nameof(EntraIdHttpClientSettings.Scope))]
    [InlineData(BaseAddress, "/.default", null, false, nameof(EntraIdHttpClientSettings.Scope))]
    [InlineData(BaseAddress, "api://inventory/Products.Read", null, false, nameof(EntraIdHttpClientSettings.Scope))]
    [InlineData(BaseAddress, "api://inventory/.default api://orders/.default", null, false, nameof(EntraIdHttpClientSettings.Scope))]
    [InlineData(BaseAddress, Scope, " ", false, nameof(EntraIdHttpClientSettings.AuthenticationScheme))]
    public async Task InvalidSettings_AreReportedWhenApplicationStartsAsync(string? baseAddress, string? scope, string? authenticationScheme, bool allowHttpOnLoopback, string invalidSetting)
    {
        using var host = CreateHost(baseAddress, scope, authenticationScheme, allowHttpOnLoopback);

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync(CancellationToken));

        Assert.Equal(ClientName, exception.OptionsName);
        Assert.StartsWith(invalidSetting, Assert.Single(exception.Failures));
    }

    [Theory]
    [InlineData("https://inventory.example.com/", false)]
    [InlineData("https://inventory.example.com/api/v1/", false)]
    [InlineData("https://localhost:7080/", false)]
    [InlineData("http://localhost:5080/", true)]
    [InlineData("http://127.0.0.1:5080/api/", true)]
    [InlineData("http://[::1]:5080/", true)]
    public async Task ValidSettings_AreAcceptedAsync(string baseAddress, bool allowHttpOnLoopback)
    {
        using var host = CreateHost(baseAddress, "00000003-0000-0000-c000-000000000000/.default", authenticationScheme: null, allowHttpOnLoopback);

        await host.StartAsync(CancellationToken);

        var settings = host.Services.GetRequiredService<IOptionsMonitor<EntraIdHttpClientSettings>>().Get(ClientName);
        Assert.Equal(new Uri(baseAddress), settings.BaseAddress);
        Assert.Equal(EntraIdDefaults.AuthenticationScheme, settings.AuthenticationScheme);
    }

    [Fact]
    public void InvalidArguments_AreRejectedAtRegistration()
    {
        var services = new ServiceCollection();
        var settings = CreateSettings();

        Assert.Throws<ArgumentNullException>(() => EntraIdHttpClientExtensions.AddEntraIdHttpClient(null!, ClientName, settings));
        Assert.Throws<ArgumentException>(() => services.AddEntraIdHttpClient(" ", settings));
        Assert.Throws<ArgumentNullException>(() => services.AddEntraIdHttpClient(ClientName, null!));
        Assert.Throws<ArgumentNullException>(() => EntraIdHttpClientExtensions.MarkAsIdempotent(null!));
    }

    private static IConfigurationSection CreateSettings(Action<Dictionary<string, string?>>? configure = null)
    {
        var settings = new Dictionary<string, string?>
        {
            [$"{ClientName}:BaseAddress"] = BaseAddress,
            [$"{ClientName}:Scope"] = Scope
        };

        configure?.Invoke(settings);

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build().GetSection(ClientName);
    }

    private static IHost CreateHost(string? baseAddress, string? scope, string? authenticationScheme, bool allowHttpOnLoopback)
    {
        // Settings that are not specified must be missing from the configuration: a null value would be bound to the property.
        var settings = new Dictionary<string, string?> { [$"{ClientName}:AllowHttpOnLoopback"] = allowHttpOnLoopback.ToString() };

        foreach (var (key, value) in new[] { ("BaseAddress", baseAddress), ("Scope", scope), ("AuthenticationScheme", authenticationScheme) })
        {
            if (value is not null)
            {
                settings[$"{ClientName}:{key}"] = value;
            }
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new HostBuilder().ConfigureServices(services => services.AddEntraIdHttpClient(ClientName, configuration.GetSection(ClientName))).Build();
    }

    private static void ConfigureFastRetries(HttpStandardResilienceOptions options)
    {
        options.Retry.BackoffType = DelayBackoffType.Constant;
        options.Retry.Delay = TimeSpan.Zero;
        options.Retry.UseJitter = false;
    }

    private sealed class Fixture(ServiceProvider serviceProvider, StubHttpMessageHandler? primaryHandler, IAuthorizationHeaderProvider authorizationHeaderProvider) : IAsyncDisposable
    {
        public HttpClient Client { get; } = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName);

        public StubHttpMessageHandler PrimaryHandler => primaryHandler ?? throw new InvalidOperationException("The client uses a real connection.");

        public IAuthorizationHeaderProvider AuthorizationHeaderProvider => authorizationHeaderProvider;

        /// <summary>
        /// Creates the HTTP client under test. Token acquisition is replaced by a substitute that is registered as scoped, like the real
        /// service, and the service provider validates scopes. When <paramref name="primaryHandler"/> is <see langword="null"/>, requests are sent over the network.
        /// </summary>
        public static Fixture Create(StubHttpMessageHandler? primaryHandler, Action<Dictionary<string, string?>>? configureSettings = null,
            Action<HttpStandardResilienceOptions>? configureResilience = null, Action<IServiceCollection>? configureServices = null, Action<IHttpClientBuilder>? configureClient = null)
        {
            var authorizationHeaderProvider = Substitute.For<IAuthorizationHeaderProvider>();
            authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(Arg.Any<string>(), Arg.Any<AuthorizationHeaderProviderOptions>(), Arg.Any<CancellationToken>())
                .Returns($"Bearer {AccessToken}");

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped(_ => authorizationHeaderProvider);
            configureServices?.Invoke(services);

            var builder = services.AddEntraIdHttpClient(ClientName, CreateSettings(configureSettings), options =>
            {
                ConfigureFastRetries(options);
                configureResilience?.Invoke(options);
            });

            if (primaryHandler is not null)
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => primaryHandler);
            }

            configureClient?.Invoke(builder);

            return new(services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }), primaryHandler, authorizationHeaderProvider);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await serviceProvider.DisposeAsync();
        }
    }

    /// <summary>
    /// Simulates a handler that does not forward the request it receives, for example because it follows redirects by itself.
    /// </summary>
    private sealed class ReplacingRequestHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var newRequest = new HttpRequestMessage(request.Method, "https://other.example.com/api/products");
            return await base.SendAsync(newRequest, cancellationToken);
        }
    }

    /// <summary>
    /// Simulates service discovery, that replaces the logical address of a service with the address of one of its instances.
    /// </summary>
    private sealed class EndpointResolvingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri = new UriBuilder(request.RequestUri!) { Host = "10.0.0.4", Port = 8443 }.Uri;
            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>
    /// Simulates a handler that limits the duration of the requests, passing a different cancellation token to the next handlers.
    /// </summary>
    private sealed class CancellingHandler(TimeSpan delay) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(delay);

            return await base.SendAsync(request, cancellationTokenSource.Token);
        }
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(Messages);

        public void Dispose()
        {
        }

        private sealed class Logger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => messages.Enqueue(formatter(state, exception));
        }
    }
}
