using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SimpleAuthentication.Permissions;

namespace SimpleAuthentication;

/// <summary>
/// Provides extension methods for adding permission-based authorization support in ASP.NET Core.
/// </summary>
public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers services required by permission-based authorization, using the specified <typeparamref name="T"/> implementation to validate permissions.
    /// </summary>
    /// <typeparam name="T">The type implementing <see cref="IPermissionHandler"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <remarks>The <see cref="IPermissionHandler"/> implementation is registered as <see cref="ServiceLifetime.Transient"/>.</remarks>
    /// <seealso cref="IPermissionHandler"/>
    public static IServiceCollection AddPermissions<T>(this IServiceCollection services) where T : class, IPermissionHandler
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddTransient<IPermissionHandler, T>();

        return services;
    }

    /// <summary>
    /// Registers services required by permission-based authorization, using the default <see cref="ScopeClaimPermissionHandler"/> implementation that uses scopes to validate permissions.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <remarks>The <see cref="ScopeClaimPermissionHandler"/> is registered as <see cref="ServiceLifetime.Transient"/>.</remarks>
    /// <seealso cref="ScopeClaimPermissionHandler"/>
    public static IServiceCollection AddScopePermissions(this IServiceCollection services)
        => services.AddPermissions<ScopeClaimPermissionHandler>();

    /// <summary>
    /// Registers services required by permission-based authorization, using the specified <typeparamref name="T"/> implementation to validates permissions.
    /// </summary>
    /// <typeparam name="T">The type implementing <see cref="IPermissionHandler"/> to register.</typeparam>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/> to add services to.</param>
    /// <returns>An <see cref="AuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <remarks>The <see cref="IPermissionHandler"/> implementation is registered as <see cref="ServiceLifetime.Transient"/>.</remarks>
    /// <seealso cref="IPermissionHandler"/>
    /// <seealso cref="AuthenticationBuilder"/>
    public static AuthenticationBuilder AddPermissions<T>(this AuthenticationBuilder builder) where T : class, IPermissionHandler
    {
        builder.Services.AddPermissions<T>();
        return builder;
    }

    /// <summary>
    /// Registers services required by permission-based authorization, using the default <see cref="ScopeClaimPermissionHandler"/> implementation that uses scopes to validate permissions.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/> to add services to.</param>
    /// <returns>An <see cref="AuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <remarks>The <see cref="ScopeClaimPermissionHandler"/> is registered as <see cref="ServiceLifetime.Transient"/>.</remarks>
    /// <seealso cref="IPermissionHandler"/>
    /// <seealso cref="ScopeClaimPermissionHandler"/>
    /// <seealso cref="AuthenticationBuilder"/>    
    public static AuthenticationBuilder AddScopePermissions(this AuthenticationBuilder builder)
    {
        builder.Services.AddPermissions<ScopeClaimPermissionHandler>();
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PermissionRequirement"/> to the <see cref="AuthorizationPolicyBuilder.Requirements"/> for this instance.
    /// </summary>
    /// <param name="builder">The <see cref="AuthorizationPolicyBuilder"/> to add policy to.</param>
    /// <param name="permissions">The list of permissions to add.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <seealso cref="AuthorizationPolicyBuilder"/>
    /// <seealso cref="PermissionRequirement"/>
    public static AuthorizationPolicyBuilder RequirePermission(this AuthorizationPolicyBuilder builder, params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddRequirements(new PermissionRequirement(permissions));
    }

    /// <summary>
    /// Adds permission-based authorization policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add policy to.</param>
    /// <param name="permissions">The permission list to require for authorization.</param>
    /// <returns>The original <see cref="IEndpointConventionBuilder"/> parameter.</returns>
    /// <seealso cref="IEndpointConventionBuilder"/>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new PermissionAttribute(permissions));
    }
}
