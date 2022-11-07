namespace SimpleAuthentication.BasicAuthentication;

/// <summary>
/// Provides methods for Basic authentication validation.
/// </summary>
public interface IBasicAuthenticationValidator
{
    /// <summary>
    /// Provides custom behavior for Basic authentication validation.
    /// </summary>
    /// <param name="userName">The user name to validate.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>The <see cref="BasicAuthenticationValidationResult"/> that contains the validation result.</returns>
    /// <seealso cref="BasicAuthenticationValidationResult"/>
    Task<BasicAuthenticationValidationResult> ValidateAsync(string userName, string password);
}
