using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SimpleAuthentication.ApiKey;

internal class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeySettings>
{
    private readonly IServiceProvider serviceProvider;

#if NET8_0_OR_GREATER
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeySettings> options, ILoggerFactory logger, UrlEncoder encoder, IServiceProvider serviceProvider)
        : base(options, logger, encoder)
#else
    public ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeySettings> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IServiceProvider serviceProvider)
        : base(options, logger, encoder, clock)
#endif
    {
        this.serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var request = Context.Request;

        var isApiKeyAvailable = (!string.IsNullOrWhiteSpace(Options.HeaderName) && request.Headers.ContainsKey(Options.HeaderName))
            || (!string.IsNullOrWhiteSpace(Options.QueryStringKey) && request.Query.ContainsKey(Options.QueryStringKey));

        if (!isApiKeyAvailable)
        {
            return AuthenticateResult.NoResult();
        }

        if (!request.Headers.TryGetValue(Options.HeaderName ?? string.Empty, out var value))
        {
            request.Query.TryGetValue(Options.QueryStringKey ?? string.Empty, out value);
        }

        if (!Options.ApiKeys.Any())
        {
            // There is no fixed values, so it tries to get an external service to validate the API Key.
            var validator = serviceProvider.GetService<IApiKeyValidator>() ?? throw new InvalidOperationException("There isn't a default value for API Key and no custom validator has been provided");

            var validationResult = await validator.ValidateAsync(value.ToString());
            if (validationResult.Succeeded)
            {
                return CreateAuthenticationSuccessResult(validationResult.UserName, validationResult.Claims);
            }

            return AuthenticateResult.Fail(validationResult.FailureMessage);
        }

        var apiKey = Options.ApiKeys.FirstOrDefault(c => c.Value == value);
        if (apiKey is not null)
        {
            return CreateAuthenticationSuccessResult(apiKey.UserName);
        }

        return AuthenticateResult.Fail("Invalid API Key");

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
}
