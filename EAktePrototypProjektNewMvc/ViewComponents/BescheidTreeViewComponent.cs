using DreiF.Shared.Abstractions;
using DreiF.Shared.Exceptions;
using EAktePrototypProjektNewMvc.Models.Tree;
using EAktePrototypProjektNewMvc.Repositories.Tree;
using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.ViewComponents
{
    public class BescheidTreeViewComponent : ViewComponent
    {
        private readonly AsyncBescheidRepository _repo;
        private readonly BuildTreeRepository _repoBuildTree;
        private readonly DcmToWs.RestClass _restClass;
        public BescheidTreeViewComponent(IConfiguration config, ICurrentMandantProvider mandantProvider)
        {
            _repo = new AsyncBescheidRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _repoBuildTree = new BuildTreeRepository(_repo);
            _restClass = new DcmToWs.RestClass();
        }

        // Überprüfen, ob der Benutzer Administrator ist und Admin-Modus aktiv ist.
        private bool IsAdminModeActive()
        {
            bool isAdmin = HttpContext.Session.GetString("Rollen")?.Split(',').Contains("Administrator") == true;
            bool adminModeActive = HttpContext.Session.GetString("AdminMode") == "true";
            return isAdmin && adminModeActive;
        }


        // Ladet die Wurzelknoten (Kategorien) und filtert sie basierend auf der Zugriffskontrolle, bevor sie an die View übergeben werden.
        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var uid = HttpContext.Session.GetString("UId");
                // ROOT - Kategorien.
                var nodes = await _repoBuildTree.GetChildren(null, null);

                var items = nodes
                    .Where(node => CheckAccess(uid, node.NodeType, node.ObjectId.Value))
                    .Select(n => new TreeItem
                    {
                        ObjectId = n.ObjectId ?? Guid.Empty,
                        Title = n.Title,
                        Url = "#",
                        NodeType = n.NodeType,
                        ParentObjectId = n.ParentObjectId,
                        Icon = n.Icon,
                        ColorHex = n.ColorHex,
                        HasChildren = n.HasChildren
                    }).ToList();

                return View(new BescheidTreeViewModel { Items = items });
            }
            catch (SharedException ex)
            {
                return View(new BescheidTreeViewModel
                {
                    Items = new(),
                    ErrorMessage = ex.Message
                });
            }
        }

        private bool CheckAccess(string uid, string nodeType, Guid objectId)
        {
            if (nodeType is "Kategorie" or "Hauptgruppe" or "Nebengruppe" or "Akte" or "Vorgang" or "Ordner" or "UnterOrdner" or "Dokument")
            {
                return _restClass.CheckAccessSecondVersion(uid, nodeType, objectId, IsAdminModeActive(), out _) > 0;
            }

            return true;
        }
    }
}
