using DreiF.Shared;
using DreiF.Shared.Exceptions;
using EAktePrototypProjektNewMvc.Models.Tree;


namespace EAktePrototypProjektNewMvc.Repositories.Tree
{
    public class BuildTreeRepository
    {
        private readonly AsyncBescheidRepository _repoAsync;

        public BuildTreeRepository(AsyncBescheidRepository repoAsync)
        {
            _repoAsync = repoAsync;
        }

        public async Task<List<TreeNode>> GetChildren(Guid? parentObjectId, string parentNodeType)
        {
            switch (parentNodeType)
            {
                case null:
                default:
                    {
                        //throw new KategorieRepositoryException("Test Fehler - Kategorien konnten nicht geladen werden.");
                        var kategorien = await SharedEAkte.GetAllKategorien();

                        return kategorien
                            .Select(kategorie => new TreeNode
                            {
                                ObjectId = kategorie.ObjektId,
                                ParentObjectId = null,
                                Title = kategorie.Bezeichnung,
                                NodeType = "Kategorie",
                                Icon = kategorie.Icon,
                                HasChildren = true
                            })
                            .ToList();
                    }
                case "Kategorie":
                    {
                        var kategorie = await SharedEAkte.GetKategorieByObjektId(objektId: parentObjectId.Value);
                        var hauptgruppen = await SharedEAkte.GetHauptgruppenByKategorieNummer(kategorieNummer: kategorie.KategorieNummer);

                        return hauptgruppen
                            .Select(hauptgruppe => new TreeNode
                            {
                                ObjectId = hauptgruppe.ObjektId,
                                ParentObjectId = parentObjectId,
                                Title = hauptgruppe.Bezeichnung,
                                NodeType = "Hauptgruppe",
                                Icon = hauptgruppe.Icon,
                                HasChildren = true
                            })
                            .ToList();
                    }
                case "Hauptgruppe":
                    {
                        var hauptgruppe = await SharedEAkte.GetHauptgruppeByObjektId(objektId: parentObjectId.Value);
                        var nebengruppen = await SharedEAkte.GetNebengruppenByHauptgruppeNummer(hauptgruppeNummer: hauptgruppe.HauptgruppeNummer);

                        return nebengruppen
                            .Select(nebengruppe => new TreeNode
                            {
                                ObjectId = nebengruppe.ObjektId,
                                ParentObjectId = parentObjectId,
                                Title = nebengruppe.Bezeichnung,
                                Icon = nebengruppe.Icon,
                                NodeType = "Nebengruppe",
                                HasChildren = true
                            })
                            .ToList();
                    }

                case "Nebengruppe":
                    {
                        var nebengruppe = await SharedEAkte.GetNebengruppeByObjektId(objektId: parentObjectId.Value);
                        var akten = await SharedEAkte.GetAktenByNebengruppeNummer(nebengruppeNummer: nebengruppe.NebengruppeNummer);

                        return akten
                            .Select(zulassung => new TreeNode
                            {
                                ObjectId = zulassung.ObjektId,
                                ParentObjectId = parentObjectId,
                                Icon = zulassung.Icon,
                                Title = zulassung.Zulassungsnummer,
                                NodeType = "Akte",
                                HasChildren = true
                            })
                            .ToList();
                    }

                case "Akte":
                    {
                        var akte = await SharedEAkte.GetAkteByObjektId(objektId: parentObjectId.Value);
                        var vorgaenge = await SharedEAkte.GetVorgaengeByZulassungsnummer(zulassungsnummer: akte.Zulassungsnummer);

                        return vorgaenge
                            .Select(vorgang => new TreeNode
                            {
                                ObjectId = vorgang.ObjektId,
                                ParentObjectId = parentObjectId,
                                Icon = vorgang.Icon,
                                Title = vorgang.Aktenzeichen,
                                NodeType = "Vorgang",
                                HasChildren = true
                            })
                            .ToList();
                    }

                case "Vorgang":
                    {
                        var vorgang = await SharedEAkte.GetVorgangByObjektId(objektId: parentObjectId.Value);

                        // Nur direkte Ordner (Type = "Ordner"), die am Vorgang hängen.
                        var ordnerr = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: vorgang.ObjektId);
                        var rootOrdner = ordnerr.Where(o => o.Type == "Ordner").ToList();

                        var folderIds = rootOrdner.Select(o => o.ObjektId).ToList();
                        var hasChildrenMap = await _repoAsync.GetFolderHasChildrenMap(folderIds: folderIds);

                        return rootOrdner
                            .Select(ordner => new TreeNode
                            {
                                ObjectId = ordner.ObjektId,
                                ParentObjectId = parentObjectId,
                                Title = ordner.Name,
                                NodeType = "Ordner",
                                ColorHex = ordner.ColorHex,
                                HasChildren = hasChildrenMap.ContainsKey(ordner.ObjektId)
                            })
                            .OrderBy(n => n.Title)
                            .ToList();
                    }

                case "Ordner":
                case "UnterOrdner":
                    {
                        // UnterOrdner die an diesem Ordner hängen (Type = "UnterOrdner").
                        var ordnerr = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: parentObjectId.Value);
                        var unterOrdner = ordnerr.Where(o => o.Type == "UnterOrdner").ToList();

                        var dokumente = await SharedEAkte.GetDokumenteByFolderId(folderId: parentObjectId.Value);

                        var folderIds = unterOrdner.Select(o => o.ObjektId).ToList();
                        var hasChildrenMap = await _repoAsync.GetFolderHasChildrenMap(folderIds: folderIds);

                        var result = new List<TreeNode>();

                        result.AddRange(unterOrdner.Select(ordner => new TreeNode
                        {
                            ObjectId = ordner.ObjektId,
                            ParentObjectId = parentObjectId,
                            Title = ordner.Name,
                            NodeType = "UnterOrdner",
                            ColorHex = ordner.ColorHex,
                            HasChildren = hasChildrenMap.ContainsKey(ordner.ObjektId)
                        }));

                        result.AddRange(dokumente.Select(dokument => new TreeNode
                        {
                            ObjectId = dokument.ObjektId,
                            ParentObjectId = parentObjectId,
                            Title = dokument.Name,
                            NodeType = "Dokument",
                            HasChildren = false
                        }));

                        return result.OrderBy(n => n.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
                    }

                case "Mitzeichnung":
                    {
                        var vorgaenge = await SharedEAkte.GetAllVorgaenge(); 
                        return vorgaenge
                            .Select(vorgang => new TreeNode
                            {
                                ObjectId = vorgang.ObjektId,
                                ParentObjectId = null,
                                Title = vorgang.Aktenzeichen,
                                NodeType = "Vorgang",
                                Icon = vorgang.Icon,
                                HasChildren = true
                            }).ToList();
                    }
            }

            return new List<TreeNode>();
        }
    }
}



