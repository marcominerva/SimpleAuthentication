using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleAuthentication.Permissions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissions<T>(this IServiceCollection services) where T : class, IPermissionService
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddTransient<IPermissionService, T>();

        return services;
    }

    public static TBuilder RequirePermissions<TBuilder>(this TBuilder builder, params string[] permissions) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new PermissionAttribute(permissions));
    }
}
