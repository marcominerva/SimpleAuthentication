using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SimpleAuthentication.EntraId.Tests.Infrastructure;

/// <summary>
/// Simulates the endpoints of a single Microsoft Entra ID tenant (metadata, signing keys and token endpoint), so that the real
/// token validation and token acquisition code runs without accessing the network.
/// </summary>
internal sealed class FakeEntraAuthority : IDisposable
{
    private const string Host = "https://login.microsoftonline.com";

    private readonly RSA rsa = RSA.Create(2048);
    private readonly ConcurrentQueue<IReadOnlyDictionary<string, string>> tokenRequests = new();
    private readonly ConcurrentDictionary<string, string[]> applicationRoles = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool signingKeyIssuerIsPublished;

    /// <summary>
    /// Creates a new instance of the <see cref="FakeEntraAuthority"/> class.
    /// </summary>
    /// <param name="signingKeyIssuerIsPublished">
    /// <see langword="true"/> to publish the issuer of the signing keys, as Microsoft Entra ID does. In this case Microsoft.Identity.Web
    /// also verifies that the issuer of a token matches the issuer of the key that has signed it.
    /// </param>
    public FakeEntraAuthority(bool signingKeyIssuerIsPublished = true)
    {
        this.signingKeyIssuerIsPublished = signingKeyIssuerIsPublished;
        SigningCredentials = new(new RsaSecurityKey(rsa) { KeyId = Guid.NewGuid().ToString("N") }, SecurityAlgorithms.RsaSha256);
    }

    public string TenantId { get; } = Guid.NewGuid().ToString();

    public const string Instance = $"{Host}/";

    public SigningCredentials SigningCredentials { get; }

    /// <summary>
    /// Gets the form parameters of the requests received by the token endpoint.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> TokenRequests => [.. tokenRequests];

    public HttpMessageHandler CreateHandler() => new Handler(this);

    /// <summary>
    /// Sets the application roles that the token endpoint adds to the app-only tokens issued for the specified API.
    /// </summary>
    public void GrantApplicationRoles(string apiClientId, params string[] roles) => applicationRoles[apiClientId] = roles;

    public string CreateToken(string audience, IEnumerable<string>? roles = null, string? identityType = "app", string? scope = null, string? tenantId = null,
        string? issuer = null, DateTime? notBefore = null, DateTime? expires = null, bool signed = true, SigningCredentials? signingCredentials = null, string? clientId = null,
        bool version1 = false, bool hasExpiration = true)
    {
        tenantId ??= TenantId;
        clientId ??= Guid.NewGuid().ToString();

        var claims = new Dictionary<string, object>
        {
            ["tid"] = tenantId,
            ["ver"] = version1 ? "1.0" : "2.0",
            [version1 ? "appid" : "azp"] = clientId,
            ["oid"] = clientId,
            ["sub"] = clientId
        };

        if (roles is not null)
        {
            claims["roles"] = roles.ToArray();
        }

        if (identityType is not null)
        {
            claims["idtyp"] = identityType;
        }

        if (scope is not null)
        {
            claims["scp"] = scope;
        }

        var now = DateTime.UtcNow;

        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = hasExpiration }.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? (version1 ? GetVersion1Issuer(tenantId) : GetIssuer(tenantId)),
            Audience = audience,
            Claims = claims,
            IssuedAt = notBefore ?? now,
            NotBefore = notBefore ?? now.AddMinutes(-1),
            Expires = hasExpiration ? expires ?? now.AddMinutes(10) : null,
            SigningCredentials = signed ? signingCredentials ?? SigningCredentials : null
        });
    }

    public void Dispose() => rsa.Dispose();

    private static string GetIssuer(string tenantId) => $"{Host}/{tenantId}/v2.0";

    private static string GetVersion1Issuer(string tenantId) => $"https://sts.windows.net/{tenantId}/";

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        var path = uri.AbsolutePath;

        if (uri.GetLeftPart(UriPartial.Authority) == Host)
        {
            if (request.Method == HttpMethod.Get && path == $"/{TenantId}/v2.0/.well-known/openid-configuration")
            {
                return CreateResponse(new
                {
                    issuer = GetIssuer(TenantId),
                    authorization_endpoint = $"{Host}/{TenantId}/oauth2/v2.0/authorize",
                    token_endpoint = $"{Host}/{TenantId}/oauth2/v2.0/token",
                    jwks_uri = $"{Host}/{TenantId}/discovery/v2.0/keys",
                    response_types_supported = new[] { "code", "id_token", "token" },
                    subject_types_supported = new[] { "pairwise" },
                    id_token_signing_alg_values_supported = new[] { SecurityAlgorithms.RsaSha256 }
                });
            }

            if (request.Method == HttpMethod.Get && path == $"/{TenantId}/.well-known/openid-configuration")
            {
                return CreateResponse(new
                {
                    issuer = GetVersion1Issuer(TenantId),
                    authorization_endpoint = $"{Host}/{TenantId}/oauth2/authorize",
                    token_endpoint = $"{Host}/{TenantId}/oauth2/token",
                    jwks_uri = $"{Host}/{TenantId}/discovery/v2.0/keys"
                });
            }

            if (request.Method == HttpMethod.Get && path == $"/{TenantId}/discovery/v2.0/keys")
            {
                var parameters = rsa.ExportParameters(includePrivateParameters: false);

                return CreateResponse(new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = "RSA",
                            use = "sig",
                            kid = SigningCredentials.Key.KeyId,
                            n = Base64UrlEncoder.Encode(parameters.Modulus),
                            e = Base64UrlEncoder.Encode(parameters.Exponent),
                            issuer = signingKeyIssuerIsPublished ? GetIssuer("{tenantid}") : null
                        }
                    }
                });
            }

            if (request.Method == HttpMethod.Get && path == "/common/discovery/instance")
            {
                return CreateResponse(new
                {
                    tenant_discovery_endpoint = $"{Host}/{TenantId}/v2.0/.well-known/openid-configuration",
                    metadata = new[]
                    {
                        new
                        {
                            preferred_network = "login.microsoftonline.com",
                            preferred_cache = "login.windows.net",
                            aliases = new[] { "login.microsoftonline.com", "login.windows.net" }
                        }
                    }
                });
            }

            if (request.Method == HttpMethod.Post && path == $"/{TenantId}/oauth2/v2.0/token")
            {
                var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken)).ToDictionary(p => p.Key, p => p.Value.ToString());
                tokenRequests.Enqueue(form);

                var apiClientId = form["scope"].Replace("api://", string.Empty).Replace("/.default", string.Empty);
                applicationRoles.TryGetValue(apiClientId, out var roles);

                return CreateResponse(new
                {
                    token_type = "Bearer",
                    expires_in = 3600,
                    access_token = CreateToken(apiClientId, roles, clientId: form["client_id"])
                });
            }
        }

        throw new InvalidOperationException($"The fake Microsoft Entra ID authority received an unexpected request: {request.Method} {uri}");
    }

    private static HttpResponseMessage CreateResponse(object content) => new(HttpStatusCode.OK) { Content = JsonContent.Create(content) };

    private sealed class Handler(FakeEntraAuthority authority) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => authority.SendAsync(request, cancellationToken);
    }
}
