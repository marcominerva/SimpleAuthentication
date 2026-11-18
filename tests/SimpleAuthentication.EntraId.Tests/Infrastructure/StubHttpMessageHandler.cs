using System.Collections.Concurrent;
using System.Net;

namespace SimpleAuthentication.EntraId.Tests.Infrastructure;

/// <summary>
/// Replaces the network: records the requests that reach the primary handler of an HTTP client and creates the responses.
/// </summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    private readonly ConcurrentQueue<RecordedRequest> requests = new();

    public IReadOnlyList<RecordedRequest> Requests => [.. requests];

    /// <summary>
    /// Creates a handler that returns the specified status codes in sequence, repeating the last one.
    /// </summary>
    public static StubHttpMessageHandler Returning(params HttpStatusCode[] statusCodes)
    {
        var index = -1;

        return new((request, _) =>
        {
            var statusCode = statusCodes[Math.Min(Interlocked.Increment(ref index), statusCodes.Length - 1)];
            return Task.FromResult(new HttpResponseMessage(statusCode) { RequestMessage = request });
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        requests.Enqueue(new(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString()));
        return send(request, cancellationToken);
    }
}

internal sealed record RecordedRequest(HttpMethod Method, Uri RequestUri, string? Authorization);
