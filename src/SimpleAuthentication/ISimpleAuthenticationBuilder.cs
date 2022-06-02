using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthentication;

/// <summary>
/// The interface used to configure simple authentication.
/// </summary>
public interface ISimpleAuthenticationBuilder
{
    /// <summary>
    /// Gets the set of key/value application configuration properties.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the object used to configure authentication.
    /// </summary>
    public AuthenticationBuilder Builder { get; }
}
