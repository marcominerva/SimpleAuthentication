using Microsoft.Extensions.DependencyInjection;

namespace SimpleAuthentication;

internal class DefaultSimpleAuthenticationBuilder : ISimpleAuthenticationBuilder
{
    public IServiceCollection Services { get; }

    public DefaultSimpleAuthenticationBuilder(IServiceCollection services)
    {
        Services = services;
    }
}
