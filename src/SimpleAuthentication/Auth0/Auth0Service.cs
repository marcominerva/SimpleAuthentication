using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;


namespace SimpleAuthentication.Auth0;

/// <summary>
/// The auth0 service.
/// </summary>
internal class Auth0Service : IAuth0Service
{
    private readonly Auth0Settings auth0Setting;
    private readonly IHttpClientFactory httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Auth0Service"/> class.
    /// </summary>
    /// <param name="auth0SettingOptions">The auth0 setting options.</param>
    /// <param name="httpClientFactory">The http client factory.</param>
    public Auth0Service(IOptions<Auth0Settings> auth0SettingOptions, IHttpClientFactory httpClientFactory)
    {
        auth0Setting = auth0SettingOptions.Value;
        this.httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Obtains the token async.
    /// </summary>
    /// <param name="claims">The claims.</param>
    /// <returns>A Task.</returns>
    public async Task<string> ObtainTokenAsync(IList<Claim>? claims = null)
    {
        claims ??= new List<Claim>();

        var jsonObject = new
        {
            client_id = auth0Setting.ClientId,
            client_secret = auth0Setting.ClientSecret,
            audience = auth0Setting.Audience,
            grant_type = auth0Setting.GrantType
        };

        string json = JsonSerializer.Serialize(value: jsonObject);
        PrepareHttpClient(json, out HttpClient client, out StringContent content);

        try
        {
            HttpResponseMessage httpResponseMessage = await client.PostAsync("/oauth/token", content);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var response = httpResponseMessage.Content.ReadAsStringAsync();
                var token = JsonSerializer.Deserialize<Auth0TokenResponse>(response.Result)!;

                claims.Update(ClaimTypes.Expiration, token.ExpiresIn.ToString());
                claims.Update(ClaimTypes.AuthenticationInstant, DateTime.UtcNow.ToString());

                return token.Token;
            }

            return httpResponseMessage.ReasonPhrase!;
        }
        catch (HttpRequestException e)
        {
            throw new HttpRequestException($"Error occurred while sending the request to obtain the Jwt Token from Auth0 provider. Error {e.Message}");
            //return e.Message;
        }
    }

    #region PrivateMethod
    /// <summary>
    /// Prepares the http client.
    /// </summary>
    /// <param name="json">The json.</param>
    /// <param name="client">The client.</param>
    /// <param name="content">The content.</param>
    private void PrepareHttpClient(string json, out HttpClient client, out StringContent content)
    {
        var baseUri = new Uri($"https:/{auth0Setting.Domain}");
        content = SetContent(json);

        client = httpClientFactory.CreateClient(auth0Setting.SchemeName);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.BaseAddress = baseUri;
        client.DefaultRequestHeaders.Host = baseUri.Host;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Configure the content for an http request
    /// </summary>
    /// <param name="json">The json serialized of the body</param>
    /// <returns>the content readey for the request</returns>
    private static StringContent SetContent(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        StringContent content = new(json, Encoding.UTF8, "application/json");
        content.Headers.ContentLength = json.Length;
        return content;
    }
    #endregion
}

/// <summary>
/// The Auth0TokenResponse class.
/// </summary>
public record class Auth0TokenResponse
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string Token { get; set; }

    /// <summary>
    /// Gets or sets the expires in.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the type.
    /// </summary>
    [JsonPropertyName("token_type")]
    public string Type { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Auth0TokenResponse"/> class.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <param name="expiresIn">The expires in.</param>
    /// <param name="type">The type.</param>
    public Auth0TokenResponse(string token, int expiresIn, string type)
    {
        this.Token = token;
        this.ExpiresIn = expiresIn;
        this.Type = type;
    }
}