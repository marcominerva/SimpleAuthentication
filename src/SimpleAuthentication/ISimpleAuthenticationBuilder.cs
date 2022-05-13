using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace SimpleAuthentication;

public interface ISimpleAuthenticationBuilder
{
    public IConfiguration Configuration { get; }

    public AuthenticationBuilder Builder { get; }
}
