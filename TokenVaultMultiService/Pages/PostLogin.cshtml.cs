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
            // First ensure we got a code back; otherwise auth flow didn't complete successfully
            string code = this.HttpContext.Request.Query["code"];
            if (!String.IsNullOrWhiteSpace(code))
            {
                string tokenVaultUrl = this._configuration["TokenVaultUrl"];
                string serviceId = this.HttpContext.Request.Query["serviceId"];
                var tokenVaultClient = new TokenVault.TokenVaultClient(tokenVaultUrl, tokenVaultApiToken);
                await tokenVaultClient.SaveTokenAsync(serviceId, tokenId, code);
            }

            return this.RedirectToPage("Index");
        }
    }
}