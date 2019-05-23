using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace TokenStoreMultiService.Controllers
{
    [Route("[controller]")]
    public class LoginController : Controller
    {
        public ActionResult Index()
        {
            var redirectUrl = Url.Page("/Index");
            return this.Challenge(new AuthenticationProperties { RedirectUri = redirectUrl } );
        }
    }
}