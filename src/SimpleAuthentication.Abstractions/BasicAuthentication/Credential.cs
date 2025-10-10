namespace SimpleAuthentication.BasicAuthentication;

/// <summary>
/// Store credentials used for Basic Authentication.
/// </summary>
/// <param name="UserName">The user name</param>
/// <param name="Password">The password</param>
/// <param name="Roles">The optional list of roles to assign to the user</param>
public record class Credential(string UserName, string Password, string[]? Roles = null);
