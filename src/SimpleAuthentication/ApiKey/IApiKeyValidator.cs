namespace SimpleAuthentication.ApiKey;

public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(string apiKey);
}
