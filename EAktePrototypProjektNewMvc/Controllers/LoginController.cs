using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.Controllers
{
    public class LoginController : Controller
    {
        // Startet die Anmeldung über Global Login (SSO).
        // Ist die Session bereits gefüllt, geht es direkt weiter. Ist die SSO-Sitzung noch aktiv,
        // kommt der Benutzer ohne Passworteingabe sofort zurück.
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Open-Redirect-Schutz: nur lokale Ziele erlauben.
            string target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl
                : Url.Action("Index", "Home")!;

            return Challenge(
                new AuthenticationProperties { RedirectUri = target },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        // Logout: lokale Sitzung beenden und danach auch die SSO-Sitzung im Global Login.
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return SignOut(
                new AuthenticationProperties { RedirectUri = Url.Action("Login", "Login") },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }
    }
}