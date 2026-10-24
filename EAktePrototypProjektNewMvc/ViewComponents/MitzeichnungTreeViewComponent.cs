using DreiF.Shared.Abstractions;
using DreiF.Shared.Exceptions;
using EAktePrototypProjektNewMvc.Models.Tree;
using EAktePrototypProjektNewMvc.Repositories.Tree;
using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.ViewComponents
{
    public class MitzeichnungTreeViewComponent : ViewComponent
    {
        private readonly AsyncBescheidRepository _repo;
        private readonly BuildTreeRepository _repoBuildTree;
        private readonly DcmToWs.RestClass _restClass;
        public MitzeichnungTreeViewComponent(IConfiguration config, ICurrentMandantProvider mandantProvider)
        {
            _repo = new AsyncBescheidRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _repoBuildTree = new BuildTreeRepository(_repo);
            _restClass = new DcmToWs.RestClass();
        }

        // Ladet die Wurzelknoten (Vorgänge) und filtert sie basierend auf der Zugriffskontrolle, bevor sie an die View übergeben werden.
        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var uid = HttpContext.Session.GetString("UId");
                // ROOT - Vorgänge.
                var nodes = await _repoBuildTree.GetChildren(null, "Mitzeichnung");

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
            if (nodeType is "Vorgang" or "Ordner" or "UnterOrdner" or "Dokument")
            {
                return _restClass.CheckAccessMitzeichnung(uid, nodeType, objectId, out _) > 0;
            }

            return true;
        }
    }
}