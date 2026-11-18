using Microsoft.Net.Http.Headers;

namespace SimpleAuthentication.EntraId;

/// <summary>
/// Ensures that the requests sent by an Entra ID HTTP client can only reach the configured base address and cannot carry
/// credentials chosen by the caller.
/// </summary>
/// <remarks>
/// It is the outermost handler of the client, so it validates the request exactly as it has been created by the caller,
/// before any other handler (for example service discovery) has the opportunity to change it. Validated requests are marked,
/// so that <see cref="EntraIdTokenHandler"/> adds the access token only to them.
/// </remarks>
internal sealed class EntraIdRequestGuardHandler(string clientName, Uri baseAddress) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!IsAllowedDestination(request.RequestUri))
        {
            throw new InvalidOperationException($"The request targets a destination outside the base address configured for the Entra ID HTTP client '{clientName}'.");
        }

        if (request.Headers.NonValidated.Contains(HeaderNames.Authorization))
        {
            throw new InvalidOperationException($"The Authorization header cannot be set on the requests sent by the Entra ID HTTP client '{clientName}', because the app-only access token is added automatically.");
        }

        if (request.Headers.NonValidated.Contains(HeaderNames.Host))
        {
            throw new InvalidOperationException($"The Host header cannot be set on the requests sent by the Entra ID HTTP client '{clientName}'.");
        }

        request.Options.Set(EntraIdHttpClientKeys.Validated, true);

        return base.SendAsync(request, cancellationToken);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new NotSupportedException($"The Entra ID HTTP client '{clientName}' does not support synchronous requests, because access tokens are acquired asynchronously.");

    private bool IsAllowedDestination(Uri? requestUri)
        => requestUri is { IsAbsoluteUri: true }
            && requestUri.UserInfo.Length == 0
            && string.Equals(requestUri.Scheme, baseAddress.Scheme, StringComparison.Ordinal)
            && string.Equals(requestUri.IdnHost, baseAddress.IdnHost, StringComparison.OrdinalIgnoreCase)
            && requestUri.Port == baseAddress.Port
            && requestUri.AbsolutePath.StartsWith(baseAddress.AbsolutePath, StringComparison.Ordinal)
            && !ContainsDotSegment(requestUri.AbsolutePath);

    private static bool ContainsDotSegment(string path)
    {
        // Uri removes dot segments, but not the ones hidden behind percent-encoded slashes (for example "..%2F") or followed by
        // path parameters (for example "..;"). Servers and proxies may interpret them anyway, moving the request outside the base address.
        if (!path.Contains('%') && !path.Contains(';'))
        {
            return false;
        }

        string decodedPath;
        while ((decodedPath = Uri.UnescapeDataString(path)) != path)
        {
            path = decodedPath;
        }

        return path.Split('/', '\\').Any(segment => segment.Split(';')[0] is "." or "..");
    }
}
