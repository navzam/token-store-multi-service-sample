using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TokenStoreMultiService.TokenStore
{
    public class TokenStoreClient
    {
        private string storeUrl;
        private string apiToken;

        private static readonly HttpClient httpClient = new HttpClient();

        public TokenStoreClient(string storeUrl, string apiToken)
        {
            this.storeUrl = storeUrl;
            this.apiToken = apiToken;
        }

        public async Task<Token> CreateTokenResourceAsync(string serviceId, string tokenId)
        {
            var tokenUri = this.GetTokenUri(serviceId, tokenId);
            var request = new HttpRequestMessage(HttpMethod.Put, tokenUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiToken);
            var requestContent = JObject.FromObject(new
            {
                displayName = tokenId
            });
            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            var responseStr = await response.Content.ReadAsStringAsync();
            var tokenStoreToken = JsonConvert.DeserializeObject<Token>(responseStr);

            return tokenStoreToken;
        }

        public async Task<Token> GetTokenResourceAsync(string serviceId, string tokenId)
        {
            var tokenUri = this.GetTokenUri(serviceId, tokenId);
            var request = new HttpRequestMessage(HttpMethod.Get, tokenUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiToken);

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseStr = await response.Content.ReadAsStringAsync();
            var tokenStoreToken = JsonConvert.DeserializeObject<Token>(responseStr);

            return tokenStoreToken;
        }

        public async Task SaveTokenAsync(string serviceId, string tokenId, string code)
        {
            var uriBuilder = new UriBuilder(this.storeUrl);
            uriBuilder.Path = $"/services/{serviceId}/tokens/{tokenId}/save";
            var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiToken);
            request.Content = new StringContent(new JObject
            {
                {
                    "code", code
                }
            }.ToString(), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to commit token: {content}");
            }
        }

        private Uri GetTokenUri(string serviceId, string tokenId)
        {
            var uriBuilder = new UriBuilder(this.storeUrl);
            uriBuilder.Path = $"/services/{serviceId}/tokens/{tokenId}";
            return uriBuilder.Uri;
        }
    }
}