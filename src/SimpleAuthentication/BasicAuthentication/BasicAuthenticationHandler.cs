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

internal partial class BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationSettings> options, ILoggerFactory logger, UrlEncoder encoder, IServiceProvider serviceProvider) : AuthenticationHandler<BasicAuthenticationSettings>(options, logger, encoder)
{
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
        if (!BasicAuthorizationHeaderRegex().IsMatch(authorizationHeader))
        {
            return AuthenticateResult.Fail("Basic Authorization header is not properly formatted");
        }

        var values = Encoding.UTF8.GetString(Convert.FromBase64String(BasicAuthorizationHeaderRegex().Replace(authorizationHeader, "$1"))).Split(':', count: 2);
        var userName = values.ElementAtOrDefault(0);
        var password = values.ElementAtOrDefault(1);

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return AuthenticateResult.Fail("Invalid user name or password");
        }

        var credentials = Options.GetAllCredentials();
        if (!credentials.Any())
        {
            // There is no fixed values, so it tries to get an external service to validate user name and password.
            var validator = serviceProvider.GetService<IBasicAuthenticationValidator>() ?? throw new InvalidOperationException("There isn't a default user name and password for authentication and no custom validator has been provided");

            var validationResult = await validator.ValidateAsync(userName, password);
            if (validationResult.Succeeded)
            {
                return CreateAuthenticationSuccessResult(validationResult.UserName, validationResult.Claims);
            }

            return AuthenticateResult.Fail(validationResult.FailureMessage);
        }

        var credential = credentials.FirstOrDefault(c => c.UserName == userName && c.Password == password);
        if (credential is not null)
        {
            var claims = new List<Claim>();
            if (credential.Roles is not null)
            {
                foreach (var role in credential.Roles)
                {
                    claims.Add(new Claim(Options.RoleClaimType, role));
                }
            }
            
            return CreateAuthenticationSuccessResult(credential.UserName, claims);
        }

        return AuthenticateResult.Fail("Invalid user name or password");

        AuthenticateResult CreateAuthenticationSuccessResult(string userName, IList<Claim>? claims = null)
        {
            claims ??= [];
            claims.Update(Options.NameClaimType, userName);

            var identity = new ClaimsIdentity(claims, Scheme.Name, Options.NameClaimType, Options.RoleClaimType);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            var result = AuthenticateResult.Success(ticket);
            return result;
        }
    }

    [GeneratedRegex(@"Basic (.*)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "it-IT")]
    private static partial Regex BasicAuthorizationHeaderRegex();
}
