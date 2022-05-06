namespace SimpleAuthentication;

public class JwtSettings
{
    public string? SecurityKey { get; init; }

    public string[]? Issuers { get; init; }

    public string[]? Audiences { get; init; }

    public int AccessTokenExpirationMinutes { get; init; }
}
