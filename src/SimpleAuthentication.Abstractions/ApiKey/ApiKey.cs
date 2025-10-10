namespace SimpleAuthentication.ApiKey;

/// <summary>
/// Store API Keys for API Key Authentication
/// </summary>
/// <param name="Value">The API key value</param>
/// <param name="UserName">The user name associated with the current key</param>
/// <param name="Roles">The optional list of roles to assign to the user</param>
public record class ApiKey(string Value, string UserName, IEnumerable<string>? Roles = null);
