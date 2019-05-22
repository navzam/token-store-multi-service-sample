using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TokenStoreMultiService.Pages
{
    public class LoginModel : PageModel
    {
        public IActionResult OnGet()
        {
            var redirectUrl = Url.Page("Index");
            return this.Challenge(new AuthenticationProperties { RedirectUri = redirectUrl });
        }
    }
}