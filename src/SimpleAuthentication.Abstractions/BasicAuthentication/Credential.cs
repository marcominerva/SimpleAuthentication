namespace SimpleAuthentication.BasicAuthentication;

/// <summary>
/// Store credentials used for Basic Authentication.
/// </summary>
/// <param name="UserName">The user name</param>
/// <param name="Password">The password</param>
public record class Credential(string UserName, string Password);
