using System.Security.Claims;

namespace SimpleAuthentication.ApiKey;

public class ApiKeyValidationResult
{
    public bool Succeeded { get; }

    public string? Username { get; set; }

    public IList<Claim>? Claims { get; }

    public string? FailureMessage { get; }

    private ApiKeyValidationResult(string username, IList<Claim>? claims)
    {
        Succeeded = true;
        Username = username;
        Claims = claims;
    }

    private ApiKeyValidationResult(string failureMessage)
    {
        FailureMessage = failureMessage;
    }

    public static ApiKeyValidationResult Success(string username, IList<Claim>? claims = null)
        => new(username, claims);

    public static ApiKeyValidationResult Fail(string failureMessage)
        => new(failureMessage);
}

