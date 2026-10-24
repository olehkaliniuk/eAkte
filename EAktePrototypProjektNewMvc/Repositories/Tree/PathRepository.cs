using Microsoft.Data.SqlClient;
using DreiF.Shared;
using EAktePrototypProjektNewMvc.Models.Tree;

namespace EAktePrototypProjektNewMvc.Repositories.Tree
{
    public class PathRepository
    {
        private readonly AsyncBescheidRepository _repoAsync;

        public PathRepository(AsyncBescheidRepository repoAsync)
        {
            _repoAsync = repoAsync;
        }

        public async Task<TreeNode> GetNode(Guid objectId, string nodeType)
        {
            switch (nodeType)
            {
                case "Kategorie":
                default:
                    {
                        var kategorie = await SharedEAkte.GetKategorieByObjektId(objektId: objectId);
                        if (kategorie == null) return null;

                        return new TreeNode
                        {
                            ObjectId = kategorie.ObjektId,
                            ParentObjectId = null,
                            Title = kategorie.Bezeichnung,
                            NodeType = "Kategorie",
                            ParentNodeType = null
                        };
                    }
                case "Hauptgruppe":
                    {
                        var hauptgruppe = await SharedEAkte.GetHauptgruppeByObjektId(objektId: objectId);
                        if (hauptgruppe == null) return null;

                        var parentKategorie = await SharedEAkte.GetKategorieByKategorieNummer(kategorieNummer: hauptgruppe.GehoertZuKategorieNummer);
            
                        if (parentKategorie == null) return null;

                        return new TreeNode
                        {
                            ObjectId = hauptgruppe.ObjektId,
                            ParentObjectId = parentKategorie.ObjektId,
                            Title = hauptgruppe.Bezeichnung,
                            NodeType = "Hauptgruppe",
                            ParentNodeType = "Kategorie"
                        };
                    }

                case "Nebengruppe":
                    {
                        var nebengruppe = await SharedEAkte.GetNebengruppeByObjektId(objektId: objectId);
                        if (nebengruppe == null) return null;

                        var parentHauptgruppe = await SharedEAkte.GetHauptgruppeByHauptgruppeNummer(hauptgruppeNummer: nebengruppe.GehoertZuHauptgruppeNummer);
                        if (parentHauptgruppe == null) return null;

                        return new TreeNode
                        {
                            ObjectId = nebengruppe.ObjektId,
                            ParentObjectId = parentHauptgruppe.ObjektId,
                            Title = nebengruppe.Bezeichnung,
                            NodeType = "Nebengruppe",
                            ParentNodeType = "Hauptgruppe"
                        };
                    }

                case "Akte":
                    {
                        var akte = await SharedEAkte.GetAkteByObjektId(objektId: objectId);
                        if (akte == null) return null;

                        var parentNebengruppe = await SharedEAkte.GetNebengruppeByNebengruppeNummer(nebengruppeNummer: Convert.ToInt32(akte.NebengruppeNummer));
                        if (parentNebengruppe == null) return null;

                        return new TreeNode
                        {
                            ObjectId = akte.ObjektId,
                            ParentObjectId = parentNebengruppe.ObjektId,
                            Title = akte.Zulassungsnummer,
                            NodeType = "Akte",
                            ParentNodeType = "Nebengruppe"
                        };
                    }

                case "Vorgang":
                    {
                        var vorgang = await SharedEAkte.GetVorgangByObjektId(objektId: objectId);
                        if (vorgang == null) return null;

                        var parentAkte = await SharedEAkte.GetAkteByZulassungsnummer(zulassungsnummer: vorgang.Zulassungsnummer);
                        if (parentAkte == null) return null;

                        return new TreeNode
                        {
                            ObjectId = vorgang.ObjektId,
                            ParentObjectId = parentAkte.ObjektId,
                            Title = vorgang.Aktenzeichen,
                            NodeType = "Vorgang",
                            ParentNodeType = "Akte"
                        };
                    }

                case "Ordner":
                case "UnterOrdner":
                    {
                        var ordner = await SharedEAkte.GetOrdnerByObjektId(objektId: objectId);
                        if (ordner == null) return null;

                        // UnterOrdner — hängt an einem anderen Ordner/UnterOrdner.
                        if (ordner.Type == "UnterOrdner")
                        {
                            return new TreeNode
                            {
                                ObjectId = ordner.ObjektId,
                                ParentObjectId = ordner.ParentObjectId,
                                Title = ordner.Name,
                                NodeType = "UnterOrdner",
                                ColorHex = ordner.ColorHex,
                                ParentNodeType = "Ordner"
                            };
                        }

                        // Ordner — hängt direkt am Vorgang.
                        var vorgang = await SharedEAkte.GetVorgangByObjektId(objektId: ordner.ParentObjectId.Value);
                        if (vorgang == null) return null;

                        return new TreeNode
                        {
                            ObjectId = ordner.ObjektId,
                            ParentObjectId = vorgang.ObjektId,
                            Title = ordner.Name,
                            NodeType = "Ordner",
                            ColorHex = ordner.ColorHex,
                            ParentNodeType = "Vorgang"
                        };
                    }

                case "Dokument":
                    {
                        var dokument = await SharedEAkte.GetDokumenteByObjektId(objektId: objectId);
                        if (dokument == null)
                            return null;

                        return new TreeNode
                        {
                            ObjectId = dokument.ObjektId,
                            ParentObjectId = dokument.FolderId,
                            Title = dokument.Name,
                            NodeType = "Dokument",
                            ParentNodeType = "UnterOrdner"
                        };
                    }
            }

        }

        public class NodePathItem
        {
            public Guid? ObjectId { get; set; }
            public string? NodeType { get; set; }
            public string? Title { get; set; }
        }

        public async Task<List<NodePathItem>> BuildNodePathWithNodeTypeAsync(Guid? objectId, string nodeType)
        {
            var path = new List<NodePathItem>();
            if (objectId == Guid.Empty)
            {
                return path;
            }

            var currentObjectId = objectId;
            var currentNodeType = nodeType;

            while (currentObjectId.HasValue && !string.IsNullOrEmpty(currentNodeType))
            {
                

                var node = await GetNode(objectId: currentObjectId.Value, nodeType: currentNodeType);

                if (node == null)
                {
                    break;
                }

                path.Insert(0, new NodePathItem
                {
                    ObjectId = node.ObjectId,
                    NodeType = node.NodeType,
                    Title = node.Title
                });

                currentObjectId = node.ParentObjectId;
                currentNodeType = node.ParentNodeType;
            }

            return path;
        }


        // Ermittelt NodeType.
        public async Task<NodePathItem?> DetectNodeByObjectId(Guid objectId)
        {
            var nodeType = await _repoAsync.DetectNodeType(objectId: objectId);

            if (nodeType == null)
            {
                return null;
            }

            return new NodePathItem
            {
                ObjectId = objectId,
                NodeType = nodeType
            };
        }

    }
}
