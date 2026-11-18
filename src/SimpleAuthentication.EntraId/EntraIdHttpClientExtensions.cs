using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;
using Microsoft.Net.Http.Headers;
using SimpleAuthentication.EntraId;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for registering HTTP clients that call APIs protected by Microsoft Entra ID using app-only access tokens.
/// </summary>
public static class EntraIdHttpClientExtensions
{
    private const string DefaultScopeSuffix = "/.default";
    private const string ResilienceSectionName = "Resilience";

    /// <summary>
    /// Registers a named <see cref="HttpClient"/> that calls an API protected by Microsoft Entra ID using an app-only access token,
    /// reading configuration from the specified <see cref="IConfigurationSection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
    /// <param name="configurationSection">
    /// The <see cref="IConfigurationSection"/> that is bound to <see cref="EntraIdHttpClientSettings"/>. It can contain a <c>Resilience</c>
    /// section, bound to <see cref="HttpStandardResilienceOptions"/>, to configure timeouts, retries and circuit breaker.
    /// </param>
    /// <param name="configureResilience">An optional delegate to configure timeouts, retries and circuit breaker. It is applied after the <c>Resilience</c> configuration section.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to further configure the client, for example to bind it to a typed client using <c>AddTypedClient</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configurationSection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <remarks>
    /// <para>
    /// Tokens are acquired through Microsoft.Identity.Web, using the credentials and the token cache that the application has registered for the
    /// authentication scheme specified in <see cref="EntraIdHttpClientSettings.AuthenticationScheme"/>: call <c>EnableTokenAcquisitionToCallDownstreamApi()</c>
    /// on the builder returned by <c>AddEntraIdJwtBearer</c> and add a token cache. The token of the incoming request is never forwarded.
    /// </para>
    /// <para>
    /// The client can send requests only to addresses that start with <see cref="EntraIdHttpClientSettings.BaseAddress"/>. Requests that
    /// target a different destination, or that contain an <c>Authorization</c> or <c>Host</c> header, fail with an <see cref="InvalidOperationException"/>.
    /// Redirects are not followed and cookies are not used, when the primary handler is a <see cref="SocketsHttpHandler"/> or an <see cref="HttpClientHandler"/>.
    /// </para>
    /// <para>
    /// The client uses its own standard resilience pipeline: resilience handlers inherited from the <see cref="HttpClient"/> defaults are removed, so that retries are not multiplied,
    /// while adding or removing resilience handlers on the returned builder causes an <see cref="InvalidOperationException"/> when the client is created.
    /// Only the requests that use a safe HTTP method (<c>GET</c>, <c>HEAD</c>, <c>OPTIONS</c> and <c>TRACE</c>) are retried. The other ones can write data, so they are
    /// sent only once unless the caller explicitly declares that they are idempotent with <see cref="MarkAsIdempotent(HttpRequestMessage)"/>.
    /// </para>
    /// <para>Settings are validated when the application starts.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddEntraIdHttpClient("Inventory", builder.Configuration.GetSection("DownstreamApis:Inventory"))
    ///     .AddTypedClient&lt;InventoryClient&gt;();
    /// </code>
    /// </example>
    /// <seealso cref="EntraIdHttpClientSettings"/>
    /// <seealso cref="HttpStandardResilienceOptions"/>
    [RequiresUnreferencedCode("HTTP client settings are bound from configuration using reflection.")]
    [RequiresDynamicCode("HTTP client settings are bound from configuration using reflection.")]
    public static IHttpClientBuilder AddEntraIdHttpClient(this IServiceCollection services, string name, IConfigurationSection configurationSection, Action<HttpStandardResilienceOptions>? configureResilience = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.AddOptions<EntraIdHttpClientSettings>(name)
            .Bind(configurationSection)
            .Validate(HasValidBaseAddress, $"{nameof(EntraIdHttpClientSettings.BaseAddress)} must be an absolute HTTPS URI that ends with a trailing slash and does not contain user information, query string or fragment. HTTP is allowed only for loopback addresses, when {nameof(EntraIdHttpClientSettings.AllowHttpOnLoopback)} is true.")
            .Validate(HasValidScope, $"{nameof(EntraIdHttpClientSettings.Scope)} must contain a single scope in the '{{Application ID URI}}{DefaultScopeSuffix}' format.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.AuthenticationScheme), $"{nameof(EntraIdHttpClientSettings.AuthenticationScheme)} is required.")
            .ValidateOnStart();

        var builder = services.AddHttpClient(name, (serviceProvider, client) => client.BaseAddress = GetSettings(serviceProvider, name).BaseAddress);

        // Removes the handlers added by the HttpClient defaults or by a previous registration of the same client,
        // so that there is always a single resilience pipeline and a single token.
        builder.ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
        {
            for (var i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i] is ResilienceHandler or EntraIdRequestGuardHandler or EntraIdTokenHandler)
                {
                    handlers.RemoveAt(i);
                }
            }
        });

        var resilienceBuilder = builder.AddStandardResilienceHandler();

        var resilienceSection = configurationSection.GetSection(ResilienceSectionName);
        if (resilienceSection.Exists())
        {
            resilienceBuilder.Configure(resilienceSection);
        }

        if (configureResilience is not null)
        {
            resilienceBuilder.Configure(configureResilience);
        }

        // Applied after any other configuration, so that requests that can write data are never retried by mistake.
        services.PostConfigure<HttpStandardResilienceOptions>(resilienceBuilder.PipelineName, static options =>
        {
            var shouldHandle = options.Retry.ShouldHandle;
            options.Retry.ShouldHandle = args => args.Context.Properties.GetValue(EntraIdHttpClientKeys.RetryAllowed, false) ? shouldHandle(args) : ValueTask.FromResult(false);
        });

        builder.AddHttpMessageHandler(serviceProvider =>
        {
            var authorizationHeaderProvider = serviceProvider.GetService<IAuthorizationHeaderProvider>()
                ?? throw new InvalidOperationException($"The Entra ID HTTP client '{name}' requires the token acquisition services of Microsoft.Identity.Web. Call EnableTokenAcquisitionToCallDownstreamApi() on the builder returned by AddEntraIdJwtBearer and add a token cache, for example with AddInMemoryTokenCaches().");

            return new EntraIdTokenHandler(name, GetSettings(serviceProvider, name), authorizationHeaderProvider);
        });

        builder.ConfigureAdditionalHttpMessageHandlers((handlers, serviceProvider) => handlers.Insert(0, new EntraIdRequestGuardHandler(name, GetSettings(serviceProvider, name).BaseAddress)));

        // Applied after any other configuration, so that the access token never ends up in the logs, redirects and cookies
        // stay disabled even if the primary handler is replaced, and resilience handlers added later are detected.
        services.PostConfigure<HttpClientFactoryOptions>(name, options =>
        {
            var shouldRedactHeaderValue = options.ShouldRedactHeaderValue;
            options.ShouldRedactHeaderValue = headerName => string.Equals(headerName, HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase) || shouldRedactHeaderValue(headerName);

            options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
            {
                EnsureSingleResiliencePipeline(name, handlerBuilder.AdditionalHandlers);
                DisableRedirectsAndCookies(handlerBuilder.PrimaryHandler);
            });
        });

        return builder;
    }

    /// <summary>
    /// Declares that the request is idempotent, so that an HTTP client registered with <c>AddEntraIdHttpClient</c> can retry it in case of transient failures,
    /// even if its HTTP method is not safe.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/> to mark as idempotent.</param>
    /// <returns>The original <see cref="HttpRequestMessage"/> parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Requests that use methods like <c>POST</c>, <c>PUT</c>, <c>PATCH</c> or <c>DELETE</c> are never retried automatically, because the API may have already processed
    /// them even if the response has not been received. Use this method only if sending the same request more than once produces the same result,
    /// for example because the API supports idempotency keys.
    /// </para>
    /// <para>The content of the request must be reusable: stream-based contents cannot be sent more than once.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var request = new HttpRequestMessage(HttpMethod.Put, "products/42") { Content = JsonContent.Create(product) };
    /// using var response = await client.SendAsync(request.MarkAsIdempotent(), cancellationToken);
    /// </code>
    /// </example>
    public static HttpRequestMessage MarkAsIdempotent(this HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Options.Set(EntraIdHttpClientKeys.Idempotent, true);
        return request;
    }

    private static EntraIdHttpClientSettings GetSettings(IServiceProvider serviceProvider, string name)
        => serviceProvider.GetRequiredService<IOptionsMonitor<EntraIdHttpClientSettings>>().Get(name);

    private static void EnsureSingleResiliencePipeline(string name, IList<DelegatingHandler> handlers)
    {
        var resilienceHandlers = 0;
        var tokenHandlerIndex = -1;

        for (var i = 0; i < handlers.Count; i++)
        {
            switch (handlers[i])
            {
                case ResilienceHandler:
                    resilienceHandlers++;
                    break;

                case EntraIdTokenHandler:
                    tokenHandlerIndex = i;
                    break;
            }
        }

        // The retry strategy must run right around the token handler, which tells it whether each request can be sent again.
        if (resilienceHandlers != 1 || tokenHandlerIndex < 1 || handlers[tokenHandlerIndex - 1] is not ResilienceHandler)
        {
            throw new InvalidOperationException($"The Entra ID HTTP client '{name}' must use only the resilience pipeline added by AddEntraIdHttpClient. To change timeouts, retries and circuit breaker, use the '{ResilienceSectionName}' configuration section or the configureResilience argument instead of adding or removing resilience handlers.");
        }
    }

    private static void DisableRedirectsAndCookies(HttpMessageHandler primaryHandler)
    {
        switch (primaryHandler)
        {
            case SocketsHttpHandler socketsHttpHandler:
                socketsHttpHandler.AllowAutoRedirect = false;
                socketsHttpHandler.UseCookies = false;
                break;

            case HttpClientHandler httpClientHandler:
                httpClientHandler.AllowAutoRedirect = false;
                httpClientHandler.UseCookies = false;
                break;
        }
    }

    private static bool HasValidBaseAddress(EntraIdHttpClientSettings settings)
        => settings.BaseAddress is { IsAbsoluteUri: true } baseAddress
            && baseAddress.UserInfo.Length == 0
            && baseAddress.Query.Length == 0
            && baseAddress.Fragment.Length == 0
            && baseAddress.AbsolutePath.EndsWith('/')
            && (baseAddress.Scheme == Uri.UriSchemeHttps || (settings.AllowHttpOnLoopback && baseAddress.Scheme == Uri.UriSchemeHttp && baseAddress.IsLoopback));

    private static bool HasValidScope(EntraIdHttpClientSettings settings)
        => settings.Scope is { Length: > 0 } scope
            && scope.Length > DefaultScopeSuffix.Length
            && scope.EndsWith(DefaultScopeSuffix, StringComparison.Ordinal)
            && !scope.Any(char.IsWhiteSpace);
}
