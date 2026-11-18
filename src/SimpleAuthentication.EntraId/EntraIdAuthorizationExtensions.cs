using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using SimpleAuthentication.EntraId;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding authorization based on Microsoft Entra ID application roles in ASP.NET Core.
/// </summary>
public static class EntraIdAuthorizationExtensions
{
    /// <summary>
    /// Requires an app-only access token, validated by the default Microsoft Entra ID authentication scheme, that contains at least one of the specified application roles.
    /// </summary>
    /// <param name="builder">The <see cref="AuthorizationPolicyBuilder"/> to add the requirement to.</param>
    /// <param name="roles">The application roles to require. The token must contain at least one of them.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="roles"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="roles"/> is empty or contains empty values.</exception>
    /// <remarks>
    /// The policy is bound to the <see cref="EntraIdDefaults.AuthenticationScheme"/> scheme, so it works even if Microsoft Entra ID
    /// is not the default authentication scheme of the application. Tokens issued on behalf of a user are rejected.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddAuthorizationBuilder()
    ///     .AddPolicy("OrdersRead", policy => policy.RequireEntraIdApplicationRole("Orders.Read"));
    /// </code>
    /// </example>
    /// <seealso cref="AuthorizationPolicyBuilder"/>
    /// <seealso cref="EntraIdApplicationRoleRequirement"/>
    public static AuthorizationPolicyBuilder RequireEntraIdApplicationRole(this AuthorizationPolicyBuilder builder, params string[] roles)
        => builder.RequireEntraIdApplicationRole(roles, EntraIdDefaults.AuthenticationScheme);

    /// <summary>
    /// Requires an app-only access token, validated by the specified Microsoft Entra ID authentication scheme, that contains at least one of the specified application roles.
    /// </summary>
    /// <param name="builder">The <see cref="AuthorizationPolicyBuilder"/> to add the requirement to.</param>
    /// <param name="roles">The application roles to require. The token must contain at least one of them.</param>
    /// <param name="schemeName">The name of the Microsoft Entra ID authentication scheme, as specified when calling <c>AddEntraIdJwtBearer</c>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="roles"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="schemeName"/> is empty, or <paramref name="roles"/> is empty or contains empty values.</exception>
    /// <remarks>
    /// The scheme is added to the <see cref="AuthorizationPolicyBuilder.AuthenticationSchemes"/> of the policy, so it is used to authenticate
    /// the request even if it is not the default one. Tokens issued on behalf of a user are rejected.
    /// </remarks>
    /// <seealso cref="AuthorizationPolicyBuilder"/>
    /// <seealso cref="EntraIdApplicationRoleRequirement"/>
    public static AuthorizationPolicyBuilder RequireEntraIdApplicationRole(this AuthorizationPolicyBuilder builder, IEnumerable<string> roles, string schemeName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var requirement = new EntraIdApplicationRoleRequirement(schemeName, roles);

        if (!builder.AuthenticationSchemes.Contains(schemeName))
        {
            builder.AddAuthenticationSchemes(schemeName);
        }

        return builder.AddRequirements(requirement);
    }

    /// <summary>
    /// Adds to the endpoint(s) an authorization policy that requires an app-only access token, validated by the default Microsoft Entra ID authentication scheme,
    /// that contains at least one of the specified application roles.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add policy to.</param>
    /// <param name="roles">The application roles to require. The token must contain at least one of them.</param>
    /// <returns>The original <see cref="IEndpointConventionBuilder"/> parameter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="roles"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="roles"/> is empty or contains empty values.</exception>
    /// <remarks>
    /// As for any other policy added using <c>RequireAuthorization</c>, the requirements are evaluated together with the ones
    /// of the default authorization policy of the application.
    /// </remarks>
    /// <example>
    /// <code>
    /// app.MapGet("api/orders", () => TypedResults.Ok())
    ///     .RequireEntraIdApplicationRole("Orders.Read");
    /// </code>
    /// </example>
    /// <seealso cref="IEndpointConventionBuilder"/>
    public static TBuilder RequireEntraIdApplicationRole<TBuilder>(this TBuilder builder, params string[] roles) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(policy => policy.RequireEntraIdApplicationRole(roles));
    }
}
