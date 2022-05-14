namespace SimpleAuthentication.JwtBearer;

public class JwtBearerSettings
{
    public string Algorithm { get; set; } = "HS256";

    public string SecurityKey { get; init; } = null!;

    public string[]? Issuers { get; init; }

    public string[]? Audiences { get; init; }

    public TimeSpan? ExpirationTime { get; init; }

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(5);

    public bool EnableJwtBearerService { get; init; } = true;
}
