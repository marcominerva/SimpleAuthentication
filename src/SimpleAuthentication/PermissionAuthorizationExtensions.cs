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
    /// Registers services required by permission-based authorization, using the specified <typeparamref name="T"/> implementation to validates permissions.
    /// </summary>
    /// <typeparam name="T">The type implementing <see cref="IPermissionHandler"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
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
    /// Registers services required by permission-based authorization, using the specified <typeparamref name="T"/> implementation to validates permissions.
    /// </summary>
    /// <typeparam name="T">The type implementing <see cref="IPermissionHandler"/> to register.</typeparam>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/> to add services to.</param>
    /// <returns>An <see cref="AuthenticationBuilder"/> that can be used to further customize authentication.</returns>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="IPermissionHandler"/>
    /// <seealso cref="AuthenticationBuilder"/>
    public static AuthenticationBuilder AddPermissions<T>(this AuthenticationBuilder builder) where T : class, IPermissionHandler
    {
        builder.Services.AddPermissions<T>();
        return builder;
    }

    /// <summary>
    /// Adds permission-based authorization policy to the endpoint(s).
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add policy to.</param>
    /// <param name="permissions">The permission list to require for authorization.</param>
    /// <returns>The original <see cref="IEndpointConventionBuilder"/> parameter.</returns>
    /// <exception cref="ArgumentNullException">One or more required configuration settings are missing.</exception>
    /// <seealso cref="IEndpointConventionBuilder"/>
    public static TBuilder RequirePermissions<TBuilder>(this TBuilder builder, params string[] permissions) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new PermissionsAttribute(permissions));
    }
}
