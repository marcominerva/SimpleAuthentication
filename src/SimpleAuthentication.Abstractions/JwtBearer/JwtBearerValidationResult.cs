using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace SimpleAuthentication.JwtBearer;

/// <summary>
/// Represents the result of JWT bearer token validation.
/// </summary>
public class JwtBearerValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the token is valid.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Principal))]
    [MemberNotNullWhen(false, nameof(Exception))]
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ClaimsPrincipal"/> representing the authenticated user.
    /// </summary>
    /// <seealso cref="ClaimsPrincipal"/>"/>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="System.Exception"/> that occurred during token validation, if any.
    /// </summary>
    /// <seealso cref="System.Exception"/>
    public Exception? Exception { get; set; }
}
