using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SimpleAuthentication.Auth0
{
    /// <summary>
    /// Options class provides information needed to control Auth0 Authentication handler behavior.
    /// </summary>
    public class Auth0Settings
    {
        /// <summary>
        /// Gets or sets The authentication scheme name (Default: Bearer).
        /// </summary>
        public string SchemeName { get; set; } = JwtBearerDefaults.AuthenticationScheme;

        /// <summary>
        /// Gets or sets the cryptographic algorithm that is used to generate the digital signature (Default: RS256).
        /// </summary>
        public string Algorithm { get; set; } = "RS256";

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        public string Domain { get; set; } = null!;

        /// <summary>
        /// Gets or sets the valid audiences that will be used to check against the token's audience.
        /// </summary>
        public string Audience { get; set; } = null!;

        /// <summary>
        /// Gets or sets the client id.
        /// </summary>
        public string ClientId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        public string ClientSecret { get; set; } = null!;

        /// <summary>
        /// Gets or sets the grant type.
        /// </summary>
        public string GrantType { get; set; } = "client_credentials";
    }
}