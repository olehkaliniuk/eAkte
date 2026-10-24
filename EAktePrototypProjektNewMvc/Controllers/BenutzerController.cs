using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Helpers;
using EAktePrototypProjektNewMvc.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EAktePrototypProjektNewMvc.Controllers
{
    [SessionAuthorize]
    [Route("benutzer")]
    public class BenutzerController: Controller
    {
        private readonly LoginRepository _repo;
        private readonly ILogger<LoginController> _logger;

        public BenutzerController(IConfiguration config, ILogger<LoginController> logger)
        {
            _repo = new LoginRepository(config.GetConnectionString("DefaultConnection"));
            _logger = logger;
        }

        [HttpGet("benutzer")]
        public IActionResult Benutzer()
        {
            try
            {
                var email = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(email))
                {
                    // Wenn nicht angemeldet, zu Login weiterleiten.
                    return RedirectToAction("Login", "Account");
                }

                var user = _repo.GetUserByEmail(email);
                if (user == null)
                {
                    // Kein Benutzer gefunden.
                    return Content("No user");
                }

                return PartialView("_Benutzer", user);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Benutzer-Fehler.");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpPost("toggle-admin-mode")]
        public IActionResult ToggleAdminMode()
        {
            bool current = HttpContext.Session.GetString("AdminMode") == "true";
            bool newState = !current;

            HttpContext.Session.SetString("AdminMode", newState.ToString().ToLower());

            return Json(new { adminMode = newState });
        }
    }
}
