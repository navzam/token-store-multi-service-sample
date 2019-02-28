using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using Microsoft.Azure.Services.AppAuthentication;
using System.Text;
using Dropbox.Api;
using Microsoft.Graph;
using System.Security.Claims;

namespace TokenVaultMultiService.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private static readonly HttpClient _httpClient = new HttpClient();

        public IndexModel(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public async Task OnGet()
        {
            // Check if user is authenticated
            if (this.User.Identity.IsAuthenticated)
            {
                System.Console.WriteLine("user authenticated");
                this.ViewData["loggedIn"] = true;
                this.ViewData["userName"] = this.User.FindFirst("name").Value;
                // TODO: can't use nameidentifier b/c Token Vault doesn't support underscores in names, and nameid can have underscores
                //var nameId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var objectId = this.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

                // Get an API token to access Token Vault
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var tokenVaultApiToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://tokenvault.azure.net");
                var tokenVaultUrl = this._configuration["TokenVaultUrl"];

                // Get Token Vault token resource for Dropbox for this user (and create it if it doesn't exist)
                var tokenVaultDropboxToken = await EnsureTokenVaultTokenResource(tokenVaultUrl, "dropbox", objectId, tokenVaultApiToken);

                // Get Dropbox status from token resource and set in view data
                var isDropboxConnected = tokenVaultDropboxToken.IsStatusOk();
                this.ViewData["isDropboxConnected"] = isDropboxConnected;

                // If connected, get data from Dropbox and set in view data
                if (isDropboxConnected)
                {
                    var dropboxFiles = await GetDropboxDocuments(tokenVaultDropboxToken.value.accessToken);
                    this.ViewData["dropboxData"] = String.Join(String.Empty, dropboxFiles.ToArray());
                }
                // Otherwise, set Dropbox login URI in view data
                else
                {
                    this.ViewData["dropboxLoginUrl"] = $"{tokenVaultDropboxToken.loginUri}?PostLoginRedirectUrl=https%3A%2F%2Flocalhost%3A44304%2Fpostlogin%3FserviceId=dropbox%26tokenId={objectId}";
                }



                // Get Token Vault token resource for Graph for this user (and create it if it doesn't exist)
                var tokenVaultGraphToken = await EnsureTokenVaultTokenResource(tokenVaultUrl, "graph", objectId, tokenVaultApiToken);

                // Get Graph status from token resource and set in view data
                var isGraphConnected = tokenVaultGraphToken.IsStatusOk();
                this.ViewData["isGraphConnected"] = isGraphConnected;

                // If connected, get data from Graph and set in view data
                if (isGraphConnected)
                {
                    var graphFiles = await GetGraphDocuments(tokenVaultGraphToken.value.accessToken);
                    this.ViewData["graphData"] = String.Join(System.Environment.NewLine, graphFiles.ToArray());
                }
                // Otherwise, set Graph login URI in view data
                else
                {
                    this.ViewData["graphLoginUrl"] = $"{tokenVaultGraphToken.loginUri}?PostLoginRedirectUrl=https%3A%2F%2Flocalhost%3A44304%2Fpostlogin%3FserviceId=graph%26tokenId={objectId}";
                }



                // Associate token name with this session, so that PostLoginRedirect can verify where the login request originated
                // TODO: session could expire... maybe move this to the login endpoint
                this.HttpContext.Session.SetString("tvId", objectId);
            }
            else
            {
                System.Console.WriteLine("user not authenticated");
                this.ViewData["loggedIn"] = false;
            }
        }

        #region Token Vault API methods

        private async Task<Models.TokenVaultToken> EnsureTokenVaultTokenResource(string tokenVaultUrl, string serviceId, string tokenId, string tokenVaultApiKey)
        {
            var retrievedToken = await GetTokenResourceFromVault(tokenVaultUrl, serviceId, tokenId, tokenVaultApiKey);
            if (retrievedToken != null)
            {
                return retrievedToken;
            }

            return await CreateTokenResourceInVault(tokenVaultUrl, serviceId, tokenId, tokenVaultApiKey);
        }

        private async Task<Models.TokenVaultToken> CreateTokenResourceInVault(string tokenVaultUrl, string serviceId, string tokenId, string tokenVaultApiKey)
        {
            var uriBuilder = new UriBuilder(tokenVaultUrl);
            uriBuilder.Path = $"/services/{serviceId}/tokens/{tokenId}";
            var request = new HttpRequestMessage(HttpMethod.Put, uriBuilder.Uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenVaultApiKey);
            // TODO: might want a strongly-typed object
            var requestContent = JObject.FromObject(new
            {
                name = tokenId,
                displayName = tokenId
            });
            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");

            // TODO: need error handling on this request
            var response = await _httpClient.SendAsync(request);
            var responseStr = await response.Content.ReadAsStringAsync();
            var tokenVaultToken = JsonConvert.DeserializeObject<Models.TokenVaultToken>(responseStr);

            return tokenVaultToken;
        }

        private async Task<Models.TokenVaultToken> GetTokenResourceFromVault(string tokenVaultUrl, string serviceId, string tokenId, string tokenVaultApiKey)
        {
            var uriBuilder = new UriBuilder(tokenVaultUrl);
            uriBuilder.Path = $"/services/{serviceId}/tokens/{tokenId}";
            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenVaultApiKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseStr = await response.Content.ReadAsStringAsync();
            var tokenVaultToken = JsonConvert.DeserializeObject<Models.TokenVaultToken>(responseStr);

            return tokenVaultToken;
        }

        #endregion

        #region Service APIs

        private async Task<IEnumerable<string>> GetDropboxDocuments(string token)
        {
            // Ensure token isn't empty
            if (string.IsNullOrEmpty(token))
            {
                return Enumerable.Empty<string>();
            }

            // Create DropboxClient and get file names
            using (var dbx = new DropboxClient(token))
            {
                var files = await dbx.Files.ListFolderAsync(string.Empty);
                var fileNames = files.Entries.Select(file => file.Name);
                return fileNames;
            }
        }

        private async Task<IEnumerable<string>> GetGraphDocuments(string token)
        {
            // Ensure token isn't empty
            if (string.IsNullOrEmpty(token))
            {
                return Enumerable.Empty<string>();
            }

            // Create GraphServiceClient and get file names
            var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                return Task.CompletedTask;
            }));
            var driveItems = await graphClient.Me.Drive.Root.Children.Request().GetAsync();
            var driveItemNames = driveItems.Select(driveItem => driveItem.Name);
            return driveItemNames;
        }

        #endregion
    }
}
