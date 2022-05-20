using Microsoft.AspNetCore.Authentication;

namespace SimpleAuthentication.ApiKey;

public class ApiKeySettings : AuthenticationSchemeOptions
{
    public string SchemeName { get; set; } = null!;

    public string? HeaderName { get; set; }

    public string? QueryName { get; set; }

    public string? ApiKeyValue { get; set; }

    public string? DefaultUsername { get; set; }
}
