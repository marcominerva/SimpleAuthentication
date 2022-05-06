using Microsoft.Extensions.DependencyInjection;

namespace SimpleAuthentication;

internal class DefaultJwtAuthenticationBuilder : IJwtAuthenticationBuilder
{
    public IServiceCollection Services { get; }

    public DefaultJwtAuthenticationBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
