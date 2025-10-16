namespace SimpleAuthentication.BasicAuthentication;

/// <summary>
/// Store credentials used for Basic Authentication.
/// </summary>
/// <param name="UserName">The user name</param>
/// <param name="Password">The password</param>
/// <param name="Roles">The list of roles to assign to the user</param>
public record class Credential(string UserName, string Password, IEnumerable<string>? Roles = null)
{
    /// <inheritdoc />
    public virtual bool Equals(Credential? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return UserName == other.UserName && Password == other.Password;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(UserName, Password);
}
