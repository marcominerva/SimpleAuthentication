using Microsoft.Extensions.DependencyInjection;

namespace SimpleAuthentication;

public interface ISimpleAuthenticationBuilder
{
    IServiceCollection Services { get; }
}
