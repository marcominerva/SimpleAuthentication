using Microsoft.Extensions.DependencyInjection;

namespace SimpleAuthentication;

public interface IJwtAuthenticationBuilder
{
    IServiceCollection Services { get; }
}
