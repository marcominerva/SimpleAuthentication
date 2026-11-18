using Polly;

namespace SimpleAuthentication.EntraId;

internal static class EntraIdHttpClientKeys
{
    /// <summary>
    /// Set by the caller on a request to state that it is idempotent, even if its HTTP method is not safe.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> Idempotent = new("SimpleAuthentication.EntraId.Idempotent");

    /// <summary>
    /// Set by <see cref="EntraIdRequestGuardHandler"/> on the requests whose destination and headers have been validated.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> Validated = new("SimpleAuthentication.EntraId.Validated");

    /// <summary>
    /// Set by <see cref="EntraIdTokenHandler"/> on the resilience context of every attempt. The value tells whether the request can be retried.
    /// </summary>
    public static readonly ResiliencePropertyKey<bool> RetryAllowed = new("SimpleAuthentication.EntraId.RetryAllowed");
}
