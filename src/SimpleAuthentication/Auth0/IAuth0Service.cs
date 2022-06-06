using System.Security.Claims;

namespace SimpleAuthentication.Auth0
{
    /// <summary>
    /// Provides methods for Auth0 Bearer generation and validation.
    /// </summary>
    public interface IAuth0Service
    {
        /// <summary>
        /// Obtains a bearer token string from Auth0 provider.
        /// </summary>
        /// <param name="claims">The claims list.</param>
        /// <returns></returns>
        Task<string> ObtainTokenAsync(IList<Claim>? claims = null);
    }
}