using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace TokenStoreMultiService.Pages
{
    public class PostAuthModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private static HttpClient _httpClient = new HttpClient();

        public PostAuthModel(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string expectedTokenId = this.HttpContext.Session.GetString("tvId");
            string tokenId = this.HttpContext.Request.Query["tokenId"];
            if (tokenId != expectedTokenId)
            {
                // Call is coming from a different session, so it will not be allowed
                throw new InvalidOperationException("token ID does not match expected value, will not save");
            }

            // Ensure we got a code back; otherwise auth flow didn't complete successfully
            string code = this.HttpContext.Request.Query["code"];
            if (!String.IsNullOrWhiteSpace(code))
            {
                // Set up Token Store client
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://tokenstore.azure.net");
                string tokenStoreUrl = this._configuration["TokenStoreUrl"];
                var tokenStoreClient = new TokenStore.TokenStoreClient(tokenStoreUrl, tokenStoreApiToken);

                // Call "save" on Token Store to verify the auth flow and finalize the token
                string serviceId = this.HttpContext.Request.Query["serviceId"];
                await tokenStoreClient.SaveTokenAsync(serviceId, tokenId, code);
            }

            return this.RedirectToPage("Index");
        }
    }
}