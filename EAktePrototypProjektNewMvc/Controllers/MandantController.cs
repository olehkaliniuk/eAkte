using EAktePrototypProjektNewMvc.Models.Mandanten;
using EAktePrototypProjektNewMvc.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.Controllers
{
    public class MandantController : Controller
    {
        private readonly MandantenRepository _mandantenRepository;

        public MandantController(IConfiguration config)
        {
            _mandantenRepository = new MandantenRepository(config.GetConnectionString("DefaultConnection"));
        }

        [HttpGet]
        public IActionResult GetAllMandanten()
        {
            if (!IsAdmin())
                return StatusCode(403); 

            var mandanten = _mandantenRepository.GetAllMandanten();
            return Json(mandanten);
        }

        [HttpPost]
        public IActionResult SwitchMandant([FromForm] int mandantId)
        {
            if (!IsAdmin())
                return StatusCode(403);

            HttpContext.Session.SetInt32("ActiveMandantId", mandantId);
            return Ok();
        }


        [HttpGet]
        public IActionResult GetActiveMandant()
        {
            if (!IsAdmin())
                return StatusCode(403);

            var activeMandant = HttpContext.Session.GetInt32("ActiveMandantId");
            var loginMandant = HttpContext.Session.GetInt32("LoginMandantId");

            return Json(new { mandantId = activeMandant ?? loginMandant ?? 0 });
        }

        [HttpPost]
        public IActionResult ResetMandant()
        {
            HttpContext.Session.Remove("ActiveMandantId");
            return Ok();
        }

        private bool IsAdmin()
        {
            var rollen = HttpContext.Session.GetString("Rollen") ?? "";
            return rollen.Split(',').Contains("DreiF");
        }
    }
}