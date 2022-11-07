using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace SimpleAuthentication.BasicAuthentication;

internal class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationSettings>
{
    private readonly IServiceProvider serviceProvider;

    public BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationSettings> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IServiceProvider serviceProvider)
        : base(options, logger, encoder, clock)
    {
        this.serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var request = Context.Request;

        if (!request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            //Response.Headers.Add(HeaderNames.WWWAuthenticate, BasicAuthenticationDefaults.AuthenticationScheme);
            //return AuthenticateResult.Fail("Authorization header is missing");

            return AuthenticateResult.NoResult();
        }

        // Get Authorization header.
        var authorizationHeader = request.Headers.Authorization.ToString();
        var authorizationHeaderRegex = new Regex(@"Basic (.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        if (!authorizationHeaderRegex.IsMatch(authorizationHeader))
        {
            return AuthenticateResult.Fail("Basic Authorization header is not properly formatted");
        }

        var values = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeaderRegex.Replace(authorizationHeader, "$1"))).Split(':', count: 2);
        var userName = values.ElementAtOrDefault(0);
        var password = values.ElementAtOrDefault(1);

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return AuthenticateResult.Fail("Invalid user name or password");
        }

        if (string.IsNullOrWhiteSpace(Options.UserName) && string.IsNullOrWhiteSpace(Options.Password))
        {
            // There is no fixed value, so it tries to get an external service to validate user name and password.
            var validator = serviceProvider.GetService<IBasicAuthenticationValidator>();
            if (validator is null)
            {
                throw new InvalidOperationException("There isn't a default user name and password for authentication and no custom validator has been provided");
            }

            var validationResult = await validator.ValidateAsync(userName, password);
            if (validationResult.Succeeded)
            {
                return CreateAuthenticationSuccessResult(validationResult.UserName!, validationResult.Claims);
            }

            return AuthenticateResult.Fail(validationResult.FailureMessage!);
        }

        if (userName == Options.UserName && password == Options.Password)
        {
            return CreateAuthenticationSuccessResult(Options.UserName!);
        }

        return AuthenticateResult.Fail("Invalid user name or password");

        AuthenticateResult CreateAuthenticationSuccessResult(string userName, IList<Claim>? claims = null)
        {
            claims ??= new List<Claim>();
            claims.Update(ClaimTypes.Name, userName);

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            var result = AuthenticateResult.Success(ticket);
            return result;
        }
    }
}
