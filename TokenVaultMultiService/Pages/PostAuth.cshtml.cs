using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace TokenVaultMultiService.Pages
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
                // Set up Token Vault client
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string tokenVaultApiToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://tokenvault.azure.net");
                string tokenVaultUrl = this._configuration["TokenVaultUrl"];
                var tokenVaultClient = new TokenVault.TokenVaultClient(tokenVaultUrl, tokenVaultApiToken);

                // Call "save" on Token Vault to verify the auth flow and finalize the token
                string serviceId = this.HttpContext.Request.Query["serviceId"];
                await tokenVaultClient.SaveTokenAsync(serviceId, tokenId, code);
            }

            return this.RedirectToPage("Index");
        }
    }
}