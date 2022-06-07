using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SimpleAuthentication.Auth0;

internal class Auth0Service : IAuth0Service
{
    private readonly Auth0Settings auth0Setting;
    private readonly IHttpClientFactory httpClientFactory;

    public Auth0Service(IOptions<Auth0Settings> auth0SettingOptions, IHttpClientFactory httpClientFactory)
    {
        auth0Setting = auth0SettingOptions.Value;
        this.httpClientFactory = httpClientFactory;
    }

    public async Task<string> ObtainTokenAsync(IList<Claim>? claims = null)
    {
        string clientId = auth0Setting.ClientId;
        string clientIdSecret = auth0Setting.ClientSecret;
        var jsonObject = new
        {
            client_id = auth0Setting.ClientId,
            client_secret = auth0Setting.ClientSecret,
            audience = auth0Setting.Audience,
            grant_type = auth0Setting.GrantType
        };

        string json = JsonSerializer.Serialize(value: jsonObject);
        PrepareHttpClient(json, out HttpClient client, out StringContent content);
        HttpResponseMessage httpResponseMessage = await client.PostAsync("/oauth/token", content);

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            return null;
        }

        return await httpResponseMessage.Content.ReadAsStringAsync();
    }

    private void PrepareHttpClient(string json, out HttpClient client, out StringContent content)
    {
        try
        {
            var baseUri = new Uri(auth0Setting.Domain);

            client = httpClientFactory.CreateClient(auth0Setting.SchemeName);
            client.Timeout = TimeSpan.FromSeconds(30);

            content = SetContent(json);

            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Host = baseUri.Host;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        catch (Exception)
        {
            client = null;
            content = null;
        }
    }

    /// <summary>
    /// Configure the content for an http request
    /// </summary>
    /// <param name="json">The json serialized of the body</param>
    /// <returns>the content readey for the request</returns>
    private static StringContent? SetContent(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        StringContent content = new(json, Encoding.UTF8, "application/json");
        content.Headers.ContentLength = json.Length;
        return content;
    }
}