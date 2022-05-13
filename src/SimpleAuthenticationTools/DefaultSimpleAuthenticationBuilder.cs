using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthenticationTools;

internal class DefaultSimpleAuthenticationToolsBuilder : ISimpleAuthenticationToolsBuilder
{
    public IConfiguration Configuration { get; }

    public AuthenticationBuilder Builder { get; }

    public DefaultSimpleAuthenticationToolsBuilder(IConfiguration configuration, AuthenticationBuilder builder)
    {
        Configuration = configuration;
        Builder = builder;
    }
}
