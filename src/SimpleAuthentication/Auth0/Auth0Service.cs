using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;

namespace SimpleAuthentication.Auth0
{
    internal class Auth0Service : IAuth0Service
    {
        private readonly Auth0Settings auth0Setting;

        public Auth0Service(IOptions<Auth0Settings> auth0SettingOptions)
        {
            auth0Setting = auth0SettingOptions.Value;
        }

        public async Task<string> ObtainTokenAsync(IList<Claim>? claims = null)
        {
            string responseContent;
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(auth0Setting.Domain);
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token");
            request.Content = new StringContent("{\"client_id\":\"ipSAr24nCse9QIAlpN6nm2sYdarlaVY5\",\"client_secret\":\"dr-qxPyLT2O7eDzCdzal9CHAe-V7t-aouZWBsDNCUsCk6r-rOjrVRQtZ9zGL7wCT\",\"audience\":\"https://github.com/micheletolve\",\"grant_type\":\"client_credentials\"}",
                Encoding.UTF8,
                "application/json");

            using (var responseMessage = await httpClient.SendAsync(request))
            {
                responseContent = await responseMessage.Content.ReadAsStringAsync();
            }
            return responseContent;
        }
    }
}