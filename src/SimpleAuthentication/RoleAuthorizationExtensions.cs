using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding role-based authorization support in ASP.NET Core.
/// </summary>
public static class RoleAuthorizationExtensions
{
    /// <summary>
    /// Adds role-based authorization policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add policy to.</param>
    /// <param name="roles">The role list to require for authorization.</param>
    /// <returns>The original <see cref="IEndpointConventionBuilder"/> parameter.</returns>
    /// <seealso cref="IEndpointConventionBuilder"/>
    public static TBuilder RequireRole<TBuilder>(this TBuilder builder, params string[] roles) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new AuthorizeAttribute { Roles = string.Join(",", roles) });
    }
}
