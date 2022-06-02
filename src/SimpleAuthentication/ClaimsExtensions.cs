using System.Security.Claims;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for streamlining working with claims.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Adds or updates the claim with the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="claims">The claims list</param>
    /// <param name="type">The name of the claim to remove</param>
    /// <param name="value">The value of the claim</param>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <seealso cref="Claim"/>
    public static void Update(this IList<Claim> claims, string type, string value)
    {
        claims.Remove(type);
        claims.Add(new Claim(type, value));
    }

    /// <summary>
    /// Removes the first occurrence of the specified claim from the list.
    /// </summary>
    /// <param name="claims">The claims list</param>
    /// <param name="type">The name of the claim to remove</param>
    /// <returns> <see langword="true"/> if item was successfully removed; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if item is not found.</returns>
    /// <seealso cref="Claim"/>
    public static bool Remove(this IList<Claim> claims, string type)
    {
        var claim = claims.FirstOrDefault(c => c.Type == type);
        return claims.Remove(claim!);
    }
}
