using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SimpleAuthentication.ApiKey;
using SimpleAuthentication.JwtBearer;

namespace BasicAuthenticationSample.Authentication;

public class ApplicationAuthenticationSchemeProvider : AuthenticationSchemeProvider
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly JwtBearerSettings jwtBearerSettings;
    private readonly ApiKeySettings apiKeySettings;

    public ApplicationAuthenticationSchemeProvider(IHttpContextAccessor httpContextAccessor, IOptions<AuthenticationOptions> options,
        IOptions<JwtBearerSettings> jwtBearerSettingsOptions, IOptions<ApiKeySettings> apiKeySettingsOptions)
        : base(options)
    {
        this.httpContextAccessor = httpContextAccessor;
        jwtBearerSettings = jwtBearerSettingsOptions.Value;
        apiKeySettings = apiKeySettingsOptions.Value;
    }

    private async Task<AuthenticationScheme?> GetRequestSchemeAsync()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            throw new ArgumentNullException("The HTTP request cannot be retrieved.");
        }

        // For API requests, use Jwt Bearer Authentication.
        if (request.IsApiRequest())
        {
            return await GetSchemeAsync(jwtBearerSettings.SchemeName);
        }

        // For Services requests, use Api Key Authentication.
        if (request.IsServiceRequest())
        {
            return await GetSchemeAsync(apiKeySettings.SchemeName);
        }

        // For the other requests, return null to let the base methods
        // decide what's the best scheme based on the default schemes
        // configured in the global authentication options.
        return null;
    }

    public override async Task<AuthenticationScheme?> GetDefaultAuthenticateSchemeAsync() =>
        await GetRequestSchemeAsync() ??
        await base.GetDefaultAuthenticateSchemeAsync();

    public override async Task<AuthenticationScheme?> GetDefaultChallengeSchemeAsync() =>
        await GetRequestSchemeAsync() ??
        await base.GetDefaultChallengeSchemeAsync();

    public override async Task<AuthenticationScheme?> GetDefaultForbidSchemeAsync() =>
        await GetRequestSchemeAsync() ??
        await base.GetDefaultForbidSchemeAsync();

    public override async Task<AuthenticationScheme?> GetDefaultSignInSchemeAsync() =>
        await GetRequestSchemeAsync() ??
        await base.GetDefaultSignInSchemeAsync();

    public override async Task<AuthenticationScheme?> GetDefaultSignOutSchemeAsync() =>
        await GetRequestSchemeAsync() ??
        await base.GetDefaultSignOutSchemeAsync();
}

public static class HttpRequestExtensions
{
    public static bool IsApiRequest(this HttpRequest request)
        => request.Path.StartsWithSegments("/api");

    public static bool IsServiceRequest(this HttpRequest request)
        => request.Path.StartsWithSegments("/service");
}
