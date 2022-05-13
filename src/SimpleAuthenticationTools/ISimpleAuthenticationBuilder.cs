using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthenticationTools;

public interface ISimpleAuthenticationToolsBuilder
{
    public IConfiguration Configuration { get; }

    public AuthenticationBuilder Builder { get; }
}
