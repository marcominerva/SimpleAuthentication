namespace SimpleAuthentication.JwtBearer;

public class JwtBearerSettings
{
    public string? SecurityKey { get; init; }

    public string[]? Issuers { get; init; }

    public string[]? Audiences { get; init; }

    public TimeSpan? ExpirationTime { get; init; }

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(5);

    public bool EnableJwtBearerGeneration { get; init; } = true;
}
