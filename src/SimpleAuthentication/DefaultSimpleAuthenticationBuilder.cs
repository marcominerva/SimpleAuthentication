using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthentication;

internal class DefaultSimpleAuthenticationBuilder : ISimpleAuthenticationBuilder
{
    public IConfiguration Configuration { get; }

    public AuthenticationBuilder Builder { get; }

    public DefaultSimpleAuthenticationBuilder(IConfiguration configuration, AuthenticationBuilder builder)
    {
        Configuration = configuration;
        Builder = builder;
    }
}
