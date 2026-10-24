using DreiF.Shared.Abstractions;
using DreiF.Shared.Exceptions;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Helpers;
using EAktePrototypProjektNewMvc.Models.GlobalSearch;
using EAktePrototypProjektNewMvc.Models.Tree;
using EAktePrototypProjektNewMvc.Repositories.Tree;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Data;

namespace EAktePrototypProjektNewMvc.Controllers
{
    [SessionAuthorize]
    [Route("bescheidbearbeitung")]
    public class BescheidController : Controller
    {
        private readonly PathRepository _repoPath;
        private readonly BuildTreeRepository _repoBuildTree;
        private readonly SearchRepository _repoSearch;
        private readonly DcmToWs.RestClass _restClass;
        private readonly AsyncBescheidRepository _asyncRepo;
        private readonly ILogger<LoginController> _logger;

        private readonly ICurrentMandantProvider _mandantProvider;

        public BescheidController(IConfiguration config, ILogger<LoginController> logger, ICurrentMandantProvider mandantProvider)
        {
            var asyncRepo = new AsyncBescheidRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _asyncRepo = asyncRepo;
            _repoPath = new PathRepository(asyncRepo);
            _repoSearch = new SearchRepository(config, mandantProvider);
            _repoBuildTree = new BuildTreeRepository(asyncRepo);
            _restClass = new DcmToWs.RestClass();
            _logger = logger;
        }

        // User ID aus der Session abrufen(UId).
        private string Uid => HttpContext.Session.GetString("UId");

        // Überprüfen, ob der Benutzer Administrator ist und Admin-Modus aktiv ist.
        private bool IsAdminModeActive()
        {
            bool isAdmin = HttpContext.Session.GetString("Rollen")?.Split(',').Contains("Administrator") == true;
            bool adminModeActive = HttpContext.Session.GetString("AdminMode") == "true";
            return isAdmin && adminModeActive;
        }


        // Ladet die Kinderknoten für einen gegebenen Elternknoten, filtert sie basierend auf der Zugriffskontrolle und gibt sie als PartialView zurück.
        [HttpGet("LoadChildren")]
        public async Task<IActionResult> LoadChildren(Guid parentObjectId, string parentNodeType)
        {
            try
            {
                var nodes = await _repoBuildTree.GetChildren(parentObjectId: parentObjectId, parentNodeType: parentNodeType);

                var items = nodes.Where(node => CheckAccess(Uid, node.NodeType, node.ObjectId.Value))

                .Select(node => new TreeItem
                {
                    ObjectId = node.ObjectId ?? Guid.Empty,
                    Title = node.Title,
                    Url = "#",
                    NodeType = node.NodeType,
                    ParentObjectId = (parentObjectId),
                    Icon = node.Icon,
                    ColorHex = node.ColorHex,
                    HasChildren = node.HasChildren
                }).ToList();

                return PartialView("_TreeChildren", items);
            }
            catch (SharedException ex)
            {
                _logger.LogError(ex, "Fehler in LoadChildren");
                return StatusCode(500, ex.Message);
            }
        }


        // Zugriffskontrollmodus wechseln (Bescheidbearbeitung <-> Mitzeichnung).
        [HttpGet("set-access-mode")]
        public IActionResult SetAccessMode(string mode, string returnUrl = null)
        {
            HttpContext.Session.SetString("AccessCheckMode", mode);
            if (returnUrl != null)
            {
                return View(returnUrl);
            }

            return Ok();
        }


        // Aktuellen Zugriffskontrollmodus abrufen.
        [HttpGet("get-access-mode")]
        public IActionResult GetAccessMode()
        {
            var mode = HttpContext.Session.GetString("AccessCheckMode") ?? "Bescheidbearbeitung";
            return Json(mode);
        }


        [HttpGet("check-access-mode")]
        public IActionResult IsMitzeichnung(Guid ObjektId, string NodeType)
        {
            try
            {
                var isMitzeichnung = _asyncRepo.IsMitzeichnung(objectId: ObjektId, NodeType: NodeType);
                return Json(isMitzeichnung);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Check-access-mode-Fehler");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        // Zugriff fur den Baum uberprufen, je nach Modus (Bescheidbearbeitung oder Mitzeichnung).
        private bool CheckAccess(string uid, string nodeType, Guid objectId)
        {
            var mode = HttpContext.Session.GetString("AccessCheckMode") ?? "Mitzeichnung";

            if (mode == "Bescheidbearbeitung")
            {
                if (nodeType is "Kategorie" or "Hauptgruppe" or "Nebengruppe" or "Akte" or "Vorgang")
                {
                    return _restClass.CheckAccessSecondVersion(uid, nodeType, objectId, IsAdminModeActive(), out _) > 0;
                }
                return true;
            }
            else // Mitzeichnung
            {
                if (nodeType is "Vorgang")
                {
                    return _restClass.CheckAccessMitzeichnung(uid, nodeType, objectId, out _) > 0;
                }
                return true;
            }
        }


        // Alle Knoten für den Pfad abrufen, wenn der NodeType bekannt ist.
        [HttpGet("get-node-path-with-node-type")]
        public async Task<IActionResult> GetNodePathWithNodeType(Guid objectId, string nodeType)
        {
            try
            {
                if (objectId == Guid.Empty)
                {
                    return Json(new List<object>());
                }

                var path = await _repoPath.BuildNodePathWithNodeTypeAsync(objectId: objectId, nodeType: nodeType);

                return Json(path);
            }
            catch (SharedException ex)
            {
                _logger.LogError(ex, "Get-node-path-with-node-type-Fehler");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        // Node-Typ abrufen.
        [HttpGet("get-node-info")]
        public async Task<IActionResult> GetNodeInfo(Guid objectId)
        {
            try
            {
                if (objectId == Guid.Empty)
                {
                    return Json(null);
                }

                var node = await _repoPath.DetectNodeByObjectId(objectId: objectId);

                if (node == null)
                {
                    return Json(null);
                }

                return Json(node); // { objectId, nodeType }
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Get-node-info-Fehler");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        // Suche.
        [HttpGet("search")]
        public IActionResult Search(string name, string mode = null)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
            {
                return Json(new List<SearchResult>());
            }

            // Falls ein Modus übergeben wurde, in der Session setzen (synchron mit dem Toggle-Button).
            if (!string.IsNullOrWhiteSpace(mode))
            {
                HttpContext.Session.SetString("AccessCheckMode", mode);
            }

            var results = _repoSearch.FindObjectsByName(name);
            if (results == null || !results.Any())
            {
                return Json(new List<SearchResult>());
            }

            string uid = Uid;

            // Filtere die Ergebnisse basierend auf den Zugriffsrechten des Benutzers und dem aktuellen Modus.
            var filteredResults = results
                .Where(item => CheckAccessSearch(uid, item.NodeType, item.ObjectId))
                .Select(item => new
                {
                    item.ObjectId,
                    item.Name,
                    nodeType = item.NodeType,
                    IconBase64 = item.Icon != null ? Convert.ToBase64String(item.Icon) : null,
                    item.ColorHex
                })
                .ToList();

            return Json(filteredResults);
        }

        // Erweiterte Zugriffskontrolle für die Suche, je nach Modus (Bescheidbearbeitung oder Mitzeichnung).
        private bool CheckAccessSearch(string uid, string nodeType, Guid objectId)
        {
            var mode = HttpContext.Session.GetString("AccessCheckMode") ?? "Mitzeichnung";

            if (mode == "Bescheidbearbeitung")
            {
                if (nodeType is "Kategorie" or "Hauptgruppe" or "Nebengruppe" or "Akte" or "Vorgang" or "UnterOrdner" or "Dokument")
                    return _restClass.CheckAccessSecondVersion(uid, nodeType, objectId, IsAdminModeActive(), out _) > 0;
                return true;
            }
            else // Mitzeichnung
            {
                if (nodeType is "Kategorie" or "Hauptgruppe" or "Nebengruppe" or "Akte" or "Vorgang" or "UnterOrdner" or "Dokument")
                    return _restClass.CheckAccessMitzeichnung(uid, nodeType, objectId, out _) > 0;
                return true;
            }
        }


    }
}
