using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace TokenVaultMultiService.Pages
{
    public class PostLoginModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private static HttpClient _httpClient = new HttpClient();

        public PostLoginModel(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var expectedTokenId = this.HttpContext.Session.GetString("tvId");
            string tokenId = this.HttpContext.Request.Query["tokenId"];
            if (tokenId != expectedTokenId)
            {
                // Call is coming from a different session, so it will not be allowed
                throw new InvalidOperationException("token ID does not match expected value, will not save");
            }

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string tokenVaultApiToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://tokenvault.azure.net");

            // Call Token Vault's /save to "save" the token
            // TODO: calling /save on the token gives the access token back; can we can take advantage of that to save a call to Token Vault in Index?
            string serviceId = this.HttpContext.Request.Query["serviceId"];
            string code = this.HttpContext.Request.Query["code"];
            string tokenVaultUrl = this._configuration["TokenVaultUrl"];
            await SaveTokenVaultToken(tokenVaultUrl, serviceId, tokenId, code, tokenVaultApiToken);

            return this.RedirectToPage("Index");
        }

        private async Task SaveTokenVaultToken(string tokenVaultUrl, string serviceId, string tokenId, string code, string tokenVaultApiKey)
        {
            var uriBuilder = new UriBuilder(tokenVaultUrl);
            uriBuilder.Path = $"/services/{serviceId}/tokens/{tokenId}/save";
            var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenVaultApiKey);
            request.Content = new StringContent(new JObject
            {
                {
                    "code", code
                }
            }.ToString(), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to commit token: {content}");
            }
        }
    }
}