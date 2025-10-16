namespace SimpleAuthentication.ApiKey;

/// <summary>
/// Store API Keys for API Key Authentication
/// </summary>
/// <param name="Value">The API key value</param>
/// <param name="UserName">The user name associated with the current key</param>
/// <param name="Roles">The list of roles to assign to the user</param>
public record class ApiKey(string Value, string UserName, IEnumerable<string>? Roles = null)
{
    /// <inheritdoc />
    public virtual bool Equals(ApiKey? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Value == other.Value && UserName == other.UserName;
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Value, UserName);
}
