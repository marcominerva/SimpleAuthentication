using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace SimpleAuthentication.EntraId;

/// <summary>
/// Keeps the default authentication scheme of the application unchanged when Microsoft Entra ID schemes are added.
/// </summary>
/// <remarks>
/// When a single authentication scheme is registered and no default scheme is set, ASP.NET Core automatically uses that scheme as the default one.
/// Adding a Microsoft Entra ID scheme would silently disable this behavior and break the endpoints that rely on it, so the scheme is set explicitly.
/// </remarks>
internal sealed class EntraIdDefaultSchemePostConfigureOptions(IOptions<EntraIdSchemeOptions> entraIdSchemeOptions) : IPostConfigureOptions<AuthenticationOptions>
{
    private const string SuppressAutoDefaultSchemeSwitchName = "Microsoft.AspNetCore.Authentication.SuppressAutoDefaultScheme";

    public void PostConfigure(string? name, AuthenticationOptions options)
    {
        if (options.DefaultScheme is not null || (AppContext.TryGetSwitch(SuppressAutoDefaultSchemeSwitchName, out var isSuppressed) && isSuppressed))
        {
            return;
        }

        var entraIdSchemeNames = entraIdSchemeOptions.Value.SchemeNames;
        var otherSchemes = options.Schemes.Where(scheme => !entraIdSchemeNames.Contains(scheme.Name)).Take(2).ToList();

        if (otherSchemes is [var schemeSelectedAutomatically])
        {
            options.DefaultScheme = schemeSelectedAutomatically.Name;
        }
    }
}

/// <summary>
/// Contains the names of the Microsoft Entra ID schemes registered by the application.
/// </summary>
internal sealed class EntraIdSchemeOptions
{
    public HashSet<string> SchemeNames { get; } = [];
}
