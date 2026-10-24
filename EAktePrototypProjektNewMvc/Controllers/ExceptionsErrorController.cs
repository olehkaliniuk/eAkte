using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.Controllers
{
    public class ExceptionsErrorController : Controller
    {
        public IActionResult Index(string? message)
        {
            var friendly = TempData["FriendlyMessage"] as string ?? message;
            var details = TempData["ErrorDetails"] as string;
            var model = (FriendlyMessage: friendly, ErrorDetails: details);

            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return PartialView("ExceptionsError", model);
            }

            return View("ExceptionsError", model);
        }
    }
}