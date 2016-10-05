using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public async Task SignIn()
        {
            var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };

            await HttpContext.Authentication.ChallengeAsync(Startup.SignInPolicyId, authenticationProperties);
        }

        [HttpGet]
        public async Task ResetPassword()
        {
            var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
            await HttpContext.Authentication.ChallengeAsync(Startup.ResetPasswordPolicyId, authenticationProperties);
        }

        [HttpGet]
        public async Task EditProfile(string redirectUri = "/")
        {
            //if (User != null && User.Identity.IsAuthenticated)
            //{
            //    // get policy 
            //    var policy = User.FindFirst(Startup.AcrClaimType).Value.ToLowerInvariant();

            //    await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            //    await HttpContext.Authentication.SignOutAsync(policy);

            //    HttpContext.Response.Redirect("/Account/EditProfile?redirecturi=" + Uri.EscapeDataString(redirectUri));
            //    return;
            //}

            var authenticationProperties = new AuthenticationProperties { RedirectUri = redirectUri };
            await HttpContext.Authentication.ChallengeAsync(Startup.ProfilePolicyId, authenticationProperties);
        }

        [HttpGet]
        public async Task SignOut()
        {
            var callbackUrl = Url.Action("SignedOut", "Account", values: null, protocol: Request.Scheme);

            if (User.Identity.IsAuthenticated)
            {

                var authenticationProperties = new AuthenticationProperties { RedirectUri = callbackUrl };

                // get policy 
                var policy = User.FindFirst(Startup.AcrClaimType).Value.ToLowerInvariant();

                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.Authentication.SignOutAsync(policy, authenticationProperties);
            }
            else
            {
                HttpContext.Response.Redirect(callbackUrl);
            }
        }

        public IActionResult SignedOut()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return View();
        }
    }
}
