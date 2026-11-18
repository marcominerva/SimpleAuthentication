using System.Net.Http.Headers;
using Microsoft.Identity.Abstractions;

namespace SimpleAuthentication.EntraId;

/// <summary>
/// Adds to the requests an app-only access token acquired through Microsoft.Identity.Web.
/// </summary>
/// <remarks>
/// It runs right inside the resilience pipeline, so every attempt uses a current token (served by the token cache of the host)
/// and token acquisition is subject to the configured timeouts.
/// </remarks>
internal sealed class EntraIdTokenHandler(string clientName, EntraIdHttpClientSettings settings, IAuthorizationHeaderProvider authorizationHeaderProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // The token is added only to the requests whose destination has been validated by EntraIdRequestGuardHandler:
        // a request created by an intermediate handler is not marked as validated.
        if (!request.Options.TryGetValue(EntraIdHttpClientKeys.Validated, out var isValidated) || !isValidated)
        {
            throw new InvalidOperationException($"The Entra ID HTTP client '{clientName}' cannot add the access token to a request whose destination has not been validated. Message handlers must forward the request they receive instead of creating a new one.");
        }

        // Tells the retry strategy, which evaluates the outcome of this attempt, whether the request can be sent again.
        request.GetResilienceContext()?.Properties.Set(EntraIdHttpClientKeys.RetryAllowed, IsRetryAllowed(request));

        var options = new AuthorizationHeaderProviderOptions
        {
            AcquireTokenOptions = new()
            {
                AuthenticationOptionsName = settings.AuthenticationScheme
            }
        };

        var authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderForAppAsync(settings.Scope, options, cancellationToken);
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            // The same request instance is used for every retry and is still reachable by the caller after the response:
            // the token must not outlive the attempt it has been acquired for.
            request.Headers.Authorization = null;
        }
    }

    private static bool IsRetryAllowed(HttpRequestMessage request)
        => request.Method == HttpMethod.Get
            || request.Method == HttpMethod.Head
            || request.Method == HttpMethod.Options
            || request.Method == HttpMethod.Trace
            || (request.Options.TryGetValue(EntraIdHttpClientKeys.Idempotent, out var isIdempotent) && isIdempotent);
}
