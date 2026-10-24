using DreiF.Shared;
using DreiF.Shared.Abstractions;
using DreiF.Shared.EAkte.Vorgaenge;
using DreiF.Shared.Exceptions;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Helpers;
using EAktePrototypProjektNewMvc.Models;
using EAktePrototypProjektNewMvc.Models.Akten;
using EAktePrototypProjektNewMvc.Models.CreateAkte;
using EAktePrototypProjektNewMvc.Models.CreateAkte.Ablaufdaten;
using EAktePrototypProjektNewMvc.Models.CreateAkte.Kontakte;
using EAktePrototypProjektNewMvc.Models.Dokumente;
using EAktePrototypProjektNewMvc.Models.Dokumente.Sessions;
using EAktePrototypProjektNewMvc.Models.Gebuehre;
using EAktePrototypProjektNewMvc.Models.Hauptgruppen;
using EAktePrototypProjektNewMvc.Models.KaHaNe;
using EAktePrototypProjektNewMvc.Models.Kategorien;
using EAktePrototypProjektNewMvc.Models.Kladde;
using EAktePrototypProjektNewMvc.Models.MoveObjectCopyPaste;
using EAktePrototypProjektNewMvc.Models.Nebengruppen;
using EAktePrototypProjektNewMvc.Models.Ordner;
using EAktePrototypProjektNewMvc.Models.PDF;
using EAktePrototypProjektNewMvc.Models.Persone;
using EAktePrototypProjektNewMvc.Models.Vorgange;
using EAktePrototypProjektNewMvc.Models.Vorlagen;
using EAktePrototypProjektNewMvc.Models.Wiedervorlage;
using EAktePrototypProjektNewMvc.Repositories;
using EAktePrototypProjektNewMvc.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.SqlClient;
using MimeKit;
using MsgReader.Mime;
using MsgReader.Outlook;
using Org.BouncyCastle.Crypto.Prng;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static EAktePrototypProjektNewMvc.Repositories.eAkteRepository;

namespace EAktePrototypProjektNewMvc.Controllers
{
    [SessionAuthorize]
    [Route("bescheidbearbeitung")]
    public class HomeController : Controller
    {
        private readonly eAkteRepository _eAkteRepo;
        private readonly LoginRepository _loginRepository;
        private readonly EditSessionRepository _editSessionRepository;
        private readonly PdfConverterSessionRepository _pdfConverterSessionRepository;
        private readonly ConfigRepository _configRepo;
        private readonly TableColumnRepository _columnRepository;
        private readonly KladdeSessionRepository _kladdeSessionRepository;
        private readonly ILogger<HomeController> _logger;
        private readonly DcmToWs.RestClass _restClass;
        private readonly ICurrentMandantProvider _mandantProvider;



        public HomeController(IConfiguration config, ILogger<HomeController> logger, ICurrentMandantProvider mandantProvider)
        {
            _eAkteRepo = new eAkteRepository(config.GetConnectionString("DefaultConnection"), mandantProvider, new WordVorlagenService());
            _loginRepository = new LoginRepository(config.GetConnectionString("DefaultConnection"));
            _editSessionRepository = new EditSessionRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _pdfConverterSessionRepository = new PdfConverterSessionRepository(config.GetConnectionString("DefaultConnection"));
            _configRepo = new ConfigRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _columnRepository = new TableColumnRepository(config.GetConnectionString("DefaultConnection"));
            _kladdeSessionRepository = new KladdeSessionRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _logger = logger;
            _restClass = new DcmToWs.RestClass();
            _mandantProvider = mandantProvider;

        }

        // User ID aus der Session abrufen(UId).
        private string Uid => HttpContext.Session.GetString("UId");

        // Modus aus der Session abrufen(Mitzeichnung / Bescheidbearbeitung).
        private string mode => HttpContext.Session.GetString("AccessCheckMode");

        // Überprüfen, ob der Benutzer Administrator ist und Admin-Modus aktiv ist.
        private bool IsAdminModeActive()
        {
            bool isAdmin = HttpContext.Session.GetString("Rollen")?.Split(',').Contains("Administrator") == true;
            bool adminModeActive = HttpContext.Session.GetString("AdminMode") == "true";
            return isAdmin && adminModeActive;
        }


        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        // Tabellen.
        [HttpGet("load-kategorien")]
        public async Task<IActionResult> Kategorien(KategorieSearchModel search, int page = 1, int pageSize = 10, string sortColumn = "KategorieNummer", string sortDirection = "asc")
        {
            try
            {
                // 1. Spalten aus der DB laden (neu).
                var columns = await _columnRepository.GetColumnsAsync("Kategorie", Uid);

                var kategorienDto = await SharedEAkte.GetAllKategorien();

                //if (kategorienDto == null)
                //{
                //    return PartialView("_AccessDenied");
                //}

                // Mapping.
                var kategorien = kategorienDto.Select(dto => new Kategorie
                {
                    Id = dto.Id,
                    KategorieNummer = dto.KategorieNummer,
                    Bezeichnung = dto.Bezeichnung,
                    Kuerzel = dto.Kuerzel,
                    Beschreibung = dto.Beschreibung,
                    ObjektId = dto.ObjektId,
                    Icon = dto.Icon
                }).ToList();

                // Berechtigung.
                kategorien = kategorien.Where(k =>
                {
                    if (mode == "Mitzeichnung")
                    {
                        var result = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Kategorie", sObjectId: k.ObjektId.Value, out _);
                        return result > 0;
                    }
                    else
                    {
                        var result = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Kategorie", sObjectId: k.ObjektId.Value, IsAdminModeActive(), out _);
                        return result > 0;
                    }
                }).ToList();

                // Wenn keine Kategorie zugänglich ist.
                //if (!kategorien.Any())
                //{
                //    return PartialView("_AccessDenied");
                //}

                // Filtration.
                if (!string.IsNullOrWhiteSpace(search.Bezeichnung))
                {
                    kategorien = kategorien
                      .Where(k => k.Bezeichnung != null && k.Bezeichnung.Contains(search.Bezeichnung, StringComparison.OrdinalIgnoreCase))
                      .ToList();
                }

                if (search.KategorieNummer.HasValue)
                {
                    kategorien = kategorien
                      .Where(k => k.KategorieNummer == search.KategorieNummer.Value)
                      .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Kuerzel))
                {
                    kategorien = kategorien
                        .Where(k => k.Kuerzel != null && k.Kuerzel.Contains(search.Kuerzel, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Sortierung.
                var sortMap = new Dictionary<string, Func<Kategorie, object>>
                {
                    { "Bezeichnung", k => k.Bezeichnung },
                    { "Kuerzel", k => k.Kuerzel },
                    { "KategorieNummer", k => k.KategorieNummer }
                };

                //// sortColumn.
                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "KategorieNummer";
                //}

                // 2. sortColumn должен быть и в sortMap, и among sortierbaren Spalten (neu).
                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();

                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "KategorieNummer";
                }


                // sortDirection.
                kategorien = sortDirection == "asc"
                    ? kategorien.OrderBy(sortMap[sortColumn]).ToList()
                    : kategorien.OrderByDescending(sortMap[sortColumn]).ToList();

                var totalCount = kategorien.Count;
                var pagedCategories = kategorien.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var model = new KategorieViewModel
                {
                    Kategorien = pagedCategories,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns
                };

                return PartialView("_KategorieTable", model);
            }
            catch (KategorieRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Kategorie");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-hauptgruppen")]
        public async Task<IActionResult> LoadHauptgruppen(Guid objectId, HauptgruppeSearchModel search, int page = 1, int pageSize = 10, string sortColumn = "HauptgruppeNummer", string sortDirection = "asc")
        {
            try
            {
                // Spalten laden (neu).
                var columns = await _columnRepository.GetColumnsAsync("Hauptgruppe", Uid);

                var kategorieDto = await SharedEAkte.GetKategorieByObjektId(objektId: objectId);

                if (kategorieDto == null)
                {
                    return PartialView("_AccessDenied");
                }

                // Zugriffskontrolle auf die Kategorie selbst.
                int accessResult;
                if (mode == "Mitzeichnung")
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Kategorie", sObjectId: objectId, out _);
                else
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Kategorie", sObjectId: objectId, IsAdminModeActive(), out _);

                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var kategorie = new Kategorie
                {
                    Id = kategorieDto.Id,
                    KategorieNummer = kategorieDto.KategorieNummer,
                    Bezeichnung = kategorieDto.Bezeichnung,
                    Kuerzel = kategorieDto.Kuerzel,
                    Beschreibung = kategorieDto.Beschreibung,
                    ObjektId = kategorieDto.ObjektId,
                    Icon = kategorieDto.Icon
                };

                var hauptgruppenDto = await SharedEAkte.GetHauptgruppenByKategorieNummer(kategorieNummer: kategorie.KategorieNummer);

                var alleHauptgruppen = hauptgruppenDto.Select(hauptgruppe => new Hauptgruppe
                {
                    Id = hauptgruppe.Id,
                    HauptgruppeNummer = hauptgruppe.HauptgruppeNummer,
                    GehoertZuKategorieNummer = hauptgruppe.GehoertZuKategorieNummer,
                    Bezeichnung = hauptgruppe.Bezeichnung,
                    Kuerzel = hauptgruppe.Kuerzel,
                    Beschreibung = hauptgruppe.Beschreibung,
                    LaufenderAktenzeichenZaehler = hauptgruppe.LaufenderAktenzeichenZaehler,
                    ObjektId = hauptgruppe.ObjektId,
                    Icon = hauptgruppe.Icon
                }).ToList();

                // Zugriffskontrolle Hauptgruppen basierend auf Person-Rolle und Hauptgruppen-ObjektId.
                alleHauptgruppen = alleHauptgruppen.Where(h =>
                {
                    if (mode == "Mitzeichnung")
                    {
                        var result = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Hauptgruppe", sObjectId: h.ObjektId.Value, out _);
                        return result > 0;
                    }
                    else
                    {
                        var result = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Hauptgruppe", sObjectId: h.ObjektId.Value, IsAdminModeActive(), out _);
                        return result > 0;
                    }
                }).ToList();

                // Search by description.
                if (!string.IsNullOrWhiteSpace(search.Bezeichnung))
                {
                    alleHauptgruppen = alleHauptgruppen
                        .Where(hauptgruppe =>
                            hauptgruppe.Bezeichnung != null &&
                            hauptgruppe.Bezeichnung.Contains(search.Bezeichnung, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Search by GehoertZuKategorieNummer (int).
                if (search.GehoertZuKategorieNummer.HasValue)
                {
                    alleHauptgruppen = alleHauptgruppen
                        .Where(hauptgruppe =>
                            hauptgruppe.GehoertZuKategorieNummer ==
                            search.GehoertZuKategorieNummer.Value)
                        .ToList();
                }

                // Search by HauptgruppeNummer (int).
                if (search.HauptgruppeNummer.HasValue)
                {
                    alleHauptgruppen = alleHauptgruppen
                        .Where(hauptgruppe =>
                            hauptgruppe.HauptgruppeNummer ==
                            search.HauptgruppeNummer.Value)
                        .ToList();
                }

                // Search by Kuerzel.
                if (!string.IsNullOrWhiteSpace(search.Kuerzel))
                {
                    alleHauptgruppen = alleHauptgruppen
                        .Where(hauptgruppe =>
                            hauptgruppe.Kuerzel != null &&
                            hauptgruppe.Kuerzel.Contains(search.Kuerzel, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Search by LaufenderAktenzeichenZaehler (int).
                if (search.LaufenderAktenzeichenZaehler.HasValue)
                {
                    alleHauptgruppen = alleHauptgruppen
                        .Where(hauptgruppe =>
                            hauptgruppe.LaufenderAktenzeichenZaehler ==
                            search.LaufenderAktenzeichenZaehler.Value)
                        .ToList();
                }

                // Sorting.
                var sortMap = new Dictionary<string, Func<Hauptgruppe, object>>
                {
                    { "GehoertZuKategorieNummer", h => h.GehoertZuKategorieNummer },
                    { "Bezeichnung", h => h.Bezeichnung },
                    { "Kuerzel", h => h.Kuerzel },
                    { "Beschreibung", h => h.Beschreibung },
                    { "HauptgruppeNummer", h => h.HauptgruppeNummer },
                    { "LaufenderAktenzeichenZaehler", h => h.LaufenderAktenzeichenZaehler },
                    { "ObjektId", h => h.ObjektId },
                    { "Id", h => h.Id }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "HauptgruppeNummer";
                //}

                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "HauptgruppeNummer";
                }

                alleHauptgruppen = sortDirection == "asc"
                    ? alleHauptgruppen.OrderBy(sortMap[sortColumn]).ToList()
                    : alleHauptgruppen.OrderByDescending(sortMap[sortColumn]).ToList();

                // Pagination.
                var totalCount = alleHauptgruppen.Count;

                var pagedHauptgruppen = alleHauptgruppen
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new HauptgruppeViewModel
                {
                    Kategorie = kategorie,
                    Hauptgruppen = pagedHauptgruppen,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns
                };

                return PartialView("_HauptgruppeTable", model);
            }
            catch (HauptgruppeRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Hauptgruppe");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-nebengruppen")]
        public async Task<IActionResult> LoadNebengruppen(Guid objectId, NebengruppeSearchModel search, int page = 1, int pageSize = 10, string sortColumn = "NebengruppeNummer", string sortDirection = "asc")
        {
            try
            {
                // Spalten laden (neu).
                var columns = await _columnRepository.GetColumnsAsync("Nebengruppe", Uid);

                var hauptgruppeDto = await SharedEAkte.GetHauptgruppeByObjektId(objektId: objectId);

                if (hauptgruppeDto == null)
                {
                    return PartialView("_AccessDenied");
                }

                // Zugriffskontrolle auf die Hauptgruppe selbst.
                int accessResult;
                if (mode == "Mitzeichnung")
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Hauptgruppe", sObjectId: hauptgruppeDto.ObjektId, out _);
                else
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Hauptgruppe", sObjectId: hauptgruppeDto.ObjektId, IsAdminModeActive(), out _);

                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var hauptgruppe = new Hauptgruppe
                {
                    Id = hauptgruppeDto.Id,
                    HauptgruppeNummer = hauptgruppeDto.HauptgruppeNummer,
                    GehoertZuKategorieNummer = hauptgruppeDto.GehoertZuKategorieNummer,
                    Bezeichnung = hauptgruppeDto.Bezeichnung,
                    Kuerzel = hauptgruppeDto.Kuerzel,
                    Beschreibung = hauptgruppeDto.Beschreibung,
                    LaufenderAktenzeichenZaehler = hauptgruppeDto.LaufenderAktenzeichenZaehler,
                    ObjektId = hauptgruppeDto.ObjektId,
                    Icon = hauptgruppeDto.Icon
                };

                var nebengruppenDto = await SharedEAkte.GetNebengruppenByHauptgruppeNummer(hauptgruppeNummer: hauptgruppeDto.HauptgruppeNummer);

                var alleNebngrupppen = nebengruppenDto.Select(nebengruppe => new Nebengruppe
                {
                    Id = nebengruppe.Id,
                    NebengruppeNummer = nebengruppe.NebengruppeNummer,
                    GehoertZuKategorieNummer = nebengruppe.GehoertZuKategorieNummer,
                    GehoertZuHauptgruppeNummer = nebengruppe.GehoertZuHauptgruppeNummer,
                    Bezeichnung = nebengruppe.Bezeichnung,
                    Kuerzel = nebengruppe.Kuerzel,
                    Beschreibung = nebengruppe.Beschreibung,
                    ObjektId = nebengruppe.ObjektId,
                    Icon = nebengruppe.Icon
                }).ToList();

                //if (mode == "Mitzeichnung")
                //{
                //    alleNebngrupppen = alleNebngrupppen.Where(n =>
                //    {
                //        var result = _restClass.CheckAccessMitzeichnung(
                //            Uid, "Nebengruppe", n.ObjektId.Value, out _);
                //        return result > 0;
                //    }).ToList();
                //}

                // Zugriffskontrolle Nebengruppen basierend auf Person-Rolle und Nebengruppen-ObjektId.
                alleNebngrupppen = alleNebngrupppen.Where(h =>
                {
                    if (mode == "Mitzeichnung")
                    {
                        var result = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Nebengruppe", sObjectId: h.ObjektId.Value, out _);
                        return result > 0;
                    }
                    else
                    {
                        var result = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Nebengruppe", sObjectId: h.ObjektId.Value, IsAdminModeActive(), out _);
                        return result > 0;
                    }
                }).ToList();

                // Search by description.
                if (!string.IsNullOrWhiteSpace(search.Bezeichnung))
                {
                    alleNebngrupppen = alleNebngrupppen
                        .Where(nebengruppe =>
                            nebengruppe.Bezeichnung != null &&
                            nebengruppe.Bezeichnung.Contains(
                                search.Bezeichnung,
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Search by GehoertZuKategorieNummer (int).
                if (search.GehoertZuKategorieNummer.HasValue)
                {
                    alleNebngrupppen = alleNebngrupppen
                        .Where(nebengruppe =>
                            nebengruppe.GehoertZuKategorieNummer ==
                            search.GehoertZuKategorieNummer.Value)
                        .ToList();
                }

                // Search by GehoertZuHauptgruppeNummer (int).
                if (search.GehoertZuHauptgruppeNummer.HasValue)
                {
                    alleNebngrupppen = alleNebngrupppen
                        .Where(nebengruppe =>
                            nebengruppe.GehoertZuHauptgruppeNummer ==
                            search.GehoertZuHauptgruppeNummer.Value)
                        .ToList();
                }

                // Search by NebengruppeNummer (int).
                if (search.NebengruppeNummer.HasValue)
                {
                    alleNebngrupppen = alleNebngrupppen
                        .Where(nebengruppe =>
                            nebengruppe.NebengruppeNummer ==
                            search.NebengruppeNummer.Value)
                        .ToList();
                }


                // Search by Kuerzel.
                if (!string.IsNullOrWhiteSpace(search.Kuerzel))
                {
                    alleNebngrupppen = alleNebngrupppen
                        .Where(nebengruppe =>
                            nebengruppe.Kuerzel != null &&
                            nebengruppe.Kuerzel.Contains(search.Kuerzel, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Sorting.
                var sortMap = new Dictionary<string, Func<Nebengruppe, object>>
                {
                    { "GehoertZuKategorieNummer", n => n.GehoertZuKategorieNummer },
                    { "GehoertZuHauptgruppeNummer", n => n.GehoertZuHauptgruppeNummer },
                    { "Bezeichnung", n => n.Bezeichnung },
                    { "Kuerzel", n => n.Kuerzel },
                    { "NebengruppeNummer", n => n.NebengruppeNummer },
                    { "Id", n => n.Id }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "NebengruppeNummer";
                //}

                // sortColumn: neu — zusätzlich gegen sichtbare/sortierbare Spalten prüfen.
                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "NebengruppeNummer";
                }

                alleNebngrupppen = sortDirection == "asc"
                    ? alleNebngrupppen.OrderBy(sortMap[sortColumn]).ToList()
                    : alleNebngrupppen.OrderByDescending(sortMap[sortColumn]).ToList();

                // Pagination.
                var totalCount = alleNebngrupppen.Count;

                var pagedNebengruppen = alleNebngrupppen
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new NebengruppeViewModel
                {
                    Nebengruppen = pagedNebengruppen,
                    Hauptgruppe = hauptgruppe,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns
                };

                return PartialView("_NebengruppeTable", model);
            }
            catch (NebengruppeRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Nebengruppe");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-akten")]
        public async Task<IActionResult> LoadAkte(AkteSearchModel search, Guid objectId, int page = 1, int pageSize = 10, string sortColumn = "Zulassungsnummer", string sortDirection = "asc")
        {
            try
            {
                // Spalten laden (neu).
                var columns = await _columnRepository.GetColumnsAsync("Akte", Uid);

                var nebengruppeDto = await SharedEAkte.GetNebengruppeByObjektId(objektId: objectId);

                if (nebengruppeDto == null)
                {
                    return PartialView("_AccessDenied");
                }

                var hauptgruppeDto = await SharedEAkte.GetHauptgruppeByHauptgruppeNummer(hauptgruppeNummer: nebengruppeDto.GehoertZuHauptgruppeNummer);


                // Zugriffskontrolle auf die Hauptgruppe selbst.
                int accessResult;
                if (mode == "Mitzeichnung")
                {
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Nebengruppe", sObjectId: nebengruppeDto.ObjektId, out _);
                }
                else
                {
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Hauptgruppe", sObjectId: hauptgruppeDto.ObjektId, IsAdminModeActive(), out _);
                }

                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var nebengruppe = new Nebengruppe
                {
                    Id = nebengruppeDto.Id,
                    NebengruppeNummer = nebengruppeDto.NebengruppeNummer,
                    GehoertZuKategorieNummer = nebengruppeDto.GehoertZuKategorieNummer,
                    GehoertZuHauptgruppeNummer = nebengruppeDto.GehoertZuHauptgruppeNummer,
                    Bezeichnung = nebengruppeDto.Bezeichnung,
                    Kuerzel = nebengruppeDto.Kuerzel,
                    Beschreibung = nebengruppeDto.Beschreibung,
                    ObjektId = nebengruppeDto.ObjektId,
                    Icon = nebengruppeDto.Icon
                };

                var aktenDto = await SharedEAkte.GetAktenByNebengruppeNummer(nebengruppeNummer: nebengruppeDto.NebengruppeNummer);

                var alleAkten = aktenDto.Select(akte => new Akte
                {
                    Zulassungsnummer = akte.Zulassungsnummer,
                    LfdNr = akte.LfdNr,
                    HauptgruppeNummer = akte.HauptgruppeNummer,
                    NebengruppeNummer = akte.NebengruppeNummer,
                    ObjektId = akte.ObjektId,
                    Zustand = akte.Zustand,
                    ErzeugtVon = akte.ErzeugtVon,
                    ErzeugtAm = akte.ErzeugtAm,
                    GeandertVon = akte.GeandertVon,
                    GeandertAm = akte.GeandertAm,
                    Icon = akte.Icon,
                    Abteilung = akte.Abteilung,
                    Referat = akte.Referat
                }).ToList();


                if (mode == "Mitzeichnung")
                {
                    alleAkten = alleAkten.Where(a =>
                    {
                        var result = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Akte", sObjectId: a.ObjektId, out _);
                        return result > 0;
                    }).ToList();
                }
                else if (mode == "Bescheidbearbeitung")
                {
                    alleAkten = alleAkten.Where(a =>
                    {
                        var result = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Akte", sObjectId: a.ObjektId, IsAdminModeActive(), out _);
                        return result > 0;
                    }).ToList();
                }

                // Filters.
                if (!string.IsNullOrWhiteSpace(search.Zulassungsnummer))
                {
                    alleAkten = alleAkten
                        .Where(a => a.Zulassungsnummer != null &&
                                    a.Zulassungsnummer.Contains(search.Zulassungsnummer, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (search.LfdNr.HasValue)
                {
                    alleAkten = alleAkten
                        .Where(a => a.LfdNr == search.LfdNr.Value)
                        .ToList();
                }



                if (!string.IsNullOrWhiteSpace(search.Zustand))
                {
                    alleAkten = alleAkten
                        .Where(a => a.Zustand != null &&
                                    a.Zustand.Contains(search.Zustand, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.ErzeugtVon))
                {
                    alleAkten = alleAkten
                        .Where(a => a.ErzeugtVon != null &&
                                    a.ErzeugtVon.Contains(search.ErzeugtVon, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Von
                if (search.ErzeugtAmVon.HasValue)
                {
                    alleAkten = alleAkten
                        .Where(a => a.ErzeugtAm.HasValue &&
                                    a.ErzeugtAm.Value.Date >= search.ErzeugtAmVon.Value.Date)
                        .ToList();
                }

                // Bis
                if (search.ErzeugtAmBis.HasValue)
                {
                    alleAkten = alleAkten
                        .Where(a => a.ErzeugtAm.HasValue &&
                                    a.ErzeugtAm.Value.Date <= search.ErzeugtAmBis.Value.Date)
                        .ToList();
                }

                // Sorting.
                var sortMap = new Dictionary<string, Func<Akte, object>>
                {
                    { "Zulassungsnummer", a => a.Zulassungsnummer },
                    { "LfdNr", a => a.LfdNr },
                    { "HauptgruppeNummer", a => a.HauptgruppeNummer },
                    { "NebengruppeNummer", a => a.NebengruppeNummer },
                    { "Zustand", a => a.Zustand },
                    { "ErzeugtVon", a => a.ErzeugtVon },
                    { "ErzeugtAm", a => a.ErzeugtAm }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "LfdNr";
                //}

                // sortColumn: zusätzlich gegen sichtbare/sortierbare Spalten prüfen (neu).
                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "Zulassungsnummer";
                }

                alleAkten = sortDirection == "asc"
                    ? alleAkten.OrderBy(sortMap[sortColumn]).ToList()
                    : alleAkten.OrderByDescending(sortMap[sortColumn]).ToList();

                // Pagination.
                var totalCount = alleAkten.Count;

                var akten = alleAkten
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

                var model = new AkteViewModel
                {
                    Nebengruppe = nebengruppe,
                    Akte = akten,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns
                };

                return PartialView("_AkteTable", model);
            }
            catch (AkteRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Akte");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-vorgange")]
        public async Task<IActionResult> LoadVorgang(VorgangSearchModel search, Guid objectId, int page = 1, int pageSize = 10, string sortColumn = "Zulassungsnummer", string sortDirection = "asc")
        {
            try
            {

                // Spalten laden (neu).
                var columns = await _columnRepository.GetColumnsAsync("Vorgang", Uid);

                var akteDto = await SharedEAkte.GetAkteByObjektId(objektId: objectId);

                if (akteDto == null)
                {
                    return PartialView("_AccessDenied");
                }

                // Zugriffskontrolle auf die Hauptgruppe selbst.
                int accessResult;
                if (mode == "Mitzeichnung")
                {
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Akte", sObjectId: akteDto.ObjektId, out _);
                }
                else
                {
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Akte", sObjectId: akteDto.ObjektId, IsAdminModeActive(), out _);
                }
                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var akte = new Akte
                {
                    Zulassungsnummer = akteDto.Zulassungsnummer,
                    LfdNr = akteDto.LfdNr,
                    HauptgruppeNummer = akteDto.HauptgruppeNummer,
                    NebengruppeNummer = akteDto.NebengruppeNummer,
                    ObjektId = akteDto.ObjektId,
                    Zustand = akteDto.Zustand,
                    ErzeugtVon = akteDto.ErzeugtVon,
                    ErzeugtAm = akteDto.ErzeugtAm,
                    GeandertVon = akteDto.GeandertVon,
                    GeandertAm = akteDto.GeandertAm,
                    Icon = akteDto.Icon,
                    Abteilung = akteDto.Abteilung,
                    Referat = akteDto.Referat
                };

                var vorganengeDto = await SharedEAkte.GetVorgaengeByZulassungsnummer(zulassungsnummer: akteDto.Zulassungsnummer);

                var alleVorgange = vorganengeDto.Select(vorgang => new Vorgang
                {
                    Aktenzeichen = vorgang.Aktenzeichen,
                    UserID = vorgang.UserID,
                    Stelle = vorgang.Stelle,
                    Zulassungsnummer = vorgang.Zulassungsnummer,

                    DatumAntrag = vorgang.DatumAntrag,
                    Antragsart = vorgang.Antragsart,
                    Bescheidtyp = vorgang.Bescheidtyp,

                    Gegenstand = vorgang.Gegenstand,
                    Status = vorgang.Status,
                    Betreff = vorgang.Betreff,
                    Bemerkung = vorgang.Bemerkung,

                    ObjektId = vorgang.ObjektId,

                    Aktenzustand = vorgang.Aktenzustand,
                    ErzeugtVon = vorgang.ErzeugtVon,
                    ErzeugtAm = vorgang.ErzeugtAm,

                    Veroeffentlicht = vorgang.Veroeffentlicht,

                    Icon = vorgang.Icon,

                    Abteilung = vorgang.Abteilung,
                    Referat = vorgang.Referat
                }).ToList();


                if (mode == "Mitzeichnung")
                {
                    alleVorgange = alleVorgange.Where(a =>
                    {
                        var result = _restClass.CheckAccessMitzeichnung(
                            Uid, "Vorgang", a.ObjektId, out _);
                        return result > 0;
                    }).ToList();
                }
                else if (mode == "Bescheidbearbeitung")
                {
                    alleVorgange = alleVorgange.Where(a =>
                    {
                        var result = _restClass.CheckAccessSecondVersion(
                            Uid, "Vorgang", a.ObjektId, IsAdminModeActive(), out _);
                        return result > 0;
                    }).ToList();
                }


                if (!string.IsNullOrWhiteSpace(search.Aktenzeichen))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Aktenzeichen != null &&
                                    v.Aktenzeichen.Contains(search.Aktenzeichen, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Zulassungsnummer))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Zulassungsnummer != null &&
                                    v.Zulassungsnummer.Contains(search.Zulassungsnummer, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }


                if (!string.IsNullOrWhiteSpace(search.Status))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Status != null &&
                                    v.Status.Contains(search.Status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }


                if (!string.IsNullOrWhiteSpace(search.Aktenzustand))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Aktenzustand != null &&
                                    v.Aktenzustand.Contains(search.Aktenzustand, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // --------------------
                // DATE RANGE – DatumAntrag
                // --------------------

                if (search.DatumAntragVon.HasValue)
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.DatumAntrag.HasValue &&
                                    v.DatumAntrag.Value.Date >= search.DatumAntragVon.Value.Date)
                        .ToList();
                }

                if (search.DatumAntragBis.HasValue)
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.DatumAntrag.HasValue &&
                                    v.DatumAntrag.Value.Date <= search.DatumAntragBis.Value.Date)
                        .ToList();
                }

                var sortMap = new Dictionary<string, Func<Vorgang, object>>
                {
                    { "Aktenzeichen", v => v.Aktenzeichen },
                    { "Zulassungsnummer", v => v.Zulassungsnummer },
                    { "Gegenstand", v => v.Gegenstand },
                    { "Status", v => v.Status },
                    { "DatumAntrag", v => v.DatumAntrag }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "DatumAntrag";
                //}


                // sortColumn: zusätzlich gegen sichtbare/sortierbare Spalten prüfen (neu).
                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "DatumAntrag";
                }

                alleVorgange = sortDirection == "asc"
                    ? alleVorgange.OrderBy(sortMap[sortColumn]).ToList()
                    : alleVorgange.OrderByDescending(sortMap[sortColumn]).ToList();

                var totalCount = alleVorgange.Count;

                var vorgange = alleVorgange
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new VorgangViewModel
                {
                    Akte = akte,
                    Vorgang = vorgange,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns,
                    ViewMode = "Standard"
                };

                return PartialView("_VorgangTable", model);
            }
            catch (VorgangRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Vorgangs");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-vorgange-mitzeichnung")] // Mitzeichnung Start-Seite.
        public async Task<IActionResult> LoadVorgangMitzeichnung(VorgangSearchModel search, Guid objectId, int page = 1, int pageSize = 10, string sortColumn = "Zulassungsnummer", string sortDirection = "asc")
        {
            try
            {

                // Spalten laden (neu).
                var columns = await _columnRepository.GetColumnsAsync("Vorgang", Uid);

                var vorganengeDto = await SharedEAkte.GetAllVorgaenge();

                if (vorganengeDto == null)
                {
                    return PartialView("_AccessDenied");
                }

                var alleVorgange = vorganengeDto.Select(vorgang => new Vorgang
                {
                    Aktenzeichen = vorgang.Aktenzeichen,
                    UserID = vorgang.UserID,
                    Stelle = vorgang.Stelle,
                    Zulassungsnummer = vorgang.Zulassungsnummer,

                    DatumAntrag = vorgang.DatumAntrag,
                    Antragsart = vorgang.Antragsart,
                    Bescheidtyp = vorgang.Bescheidtyp,

                    Gegenstand = vorgang.Gegenstand,
                    Status = vorgang.Status,
                    Betreff = vorgang.Betreff,
                    Bemerkung = vorgang.Bemerkung,

                    ObjektId = vorgang.ObjektId,

                    Aktenzustand = vorgang.Aktenzustand,
                    ErzeugtVon = vorgang.ErzeugtVon,
                    ErzeugtAm = vorgang.ErzeugtAm,

                    Veroeffentlicht = vorgang.Veroeffentlicht,

                    Icon = vorgang.Icon,

                    Abteilung = vorgang.Abteilung,
                    Referat = vorgang.Referat
                }).ToList();

                alleVorgange = alleVorgange.Where(a =>
                {
                    var result = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Vorgang", sObjectId: a.ObjektId, out _);
                    return result > 0;
                }).ToList();

                if (!string.IsNullOrWhiteSpace(search.Aktenzeichen))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Aktenzeichen != null &&
                                    v.Aktenzeichen.Contains(search.Aktenzeichen, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Zulassungsnummer))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Zulassungsnummer != null &&
                                    v.Zulassungsnummer.Contains(search.Zulassungsnummer, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }


                if (!string.IsNullOrWhiteSpace(search.Status))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Status != null &&
                                    v.Status.Contains(search.Status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }


                if (!string.IsNullOrWhiteSpace(search.Aktenzustand))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Aktenzustand != null &&
                                    v.Aktenzustand.Contains(search.Aktenzustand, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // --------------------
                // DATE RANGE – DatumAntrag
                // --------------------

                if (search.DatumAntragVon.HasValue)
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.DatumAntrag.HasValue &&
                                    v.DatumAntrag.Value.Date >= search.DatumAntragVon.Value.Date)
                        .ToList();
                }

                if (search.DatumAntragBis.HasValue)
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.DatumAntrag.HasValue &&
                                    v.DatumAntrag.Value.Date <= search.DatumAntragBis.Value.Date)
                        .ToList();
                }



                var sortMap = new Dictionary<string, Func<Vorgang, object>>
                {
                    { "Aktenzeichen", v => v.Aktenzeichen },
                    { "Zulassungsnummer", v => v.Zulassungsnummer },
                    { "Gegenstand", v => v.Gegenstand },
                    { "Status", v => v.Status },
                    { "DatumAntrag", v => v.DatumAntrag }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "DatumAntrag";
                //}

                // sortColumn: zusätzlich gegen sichtbare/sortierbare Spalten prüfen (neu).
                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "DatumAntrag";
                }

                alleVorgange = sortDirection == "asc"
                    ? alleVorgange.OrderBy(sortMap[sortColumn]).ToList()
                    : alleVorgange.OrderByDescending(sortMap[sortColumn]).ToList();

                var totalCount = alleVorgange.Count;

                var vorgange = alleVorgange
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new VorgangViewModel
                {
                    Vorgang = vorgange,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns,
                    ViewMode = "Mitzeichnung"
                };

                return PartialView("_VorgangTable", model);
            }
            catch (VorgangRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Vorgangs");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-ordner")]
        public async Task<IActionResult> LoadOrdner(OrdnerSearchModel search, Guid objectId, int page = 1, int pageSize = 10, string sortColumn = "Name", string sortDirection = "asc")
        {
            try
            {
                var columns = await _columnRepository.GetColumnsAsync("Ordner", Uid);

                var vorgangDto = await SharedEAkte.GetVorgangByObjektId(objektId: objectId);

                if (vorgangDto == null)
                {
                    return PartialView("_AccessDenied");
                }


                int accessResult;
                if (mode == "Mitzeichnung")
                {
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Vorgang", sObjectId: vorgangDto.ObjektId, out _);
                }
                else
                {
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Vorgang", sObjectId: vorgangDto.ObjektId, IsAdminModeActive(), out _);
                }

                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var vorgang = new Vorgang
                {
                    Aktenzeichen = vorgangDto.Aktenzeichen,
                    UserID = vorgangDto.UserID,
                    Abteilung = vorgangDto.Abteilung,
                    Stelle = vorgangDto.Stelle,
                    Zulassungsnummer = vorgangDto.Zulassungsnummer,

                    DatumAntrag = vorgangDto.DatumAntrag,
                    Antragsart = vorgangDto.Antragsart,
                    Bescheidtyp = vorgangDto.Bescheidtyp,

                    Gegenstand = vorgangDto.Gegenstand,
                    Status = vorgangDto.Status,
                    Betreff = vorgangDto.Betreff,
                    Bemerkung = vorgangDto.Bemerkung,

                    ObjektId = vorgangDto.ObjektId,

                    Aktenzustand = vorgangDto.Aktenzustand,
                    ErzeugtVon = vorgangDto.ErzeugtVon,
                    ErzeugtAm = vorgangDto.ErzeugtAm,

                    Veroeffentlicht = vorgangDto.Veroeffentlicht,

                    Icon = vorgangDto.Icon
                };


                var ordnerDto = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: vorgangDto.ObjektId);

                var alleOrdner = ordnerDto
                .Where(ordner => ordner.Type == "Ordner")
                .Select(ordner => new Ordner
                {
                    ObjektId = ordner.ObjektId,
                    ParentObjectId = ordner.ParentObjectId,
                    Name = ordner.Name,
                    Type = ordner.Type,
                    CreatorName = ordner.CreatorName,
                    CreationDate = ordner.CreationDate,
                    ModifyDate = ordner.ModifyDate,
                    Modifier = ordner.Modifier,
                    OwnerName = ordner.OwnerName,
                    Hauptsachgebiet = ordner.Hauptsachgebiet,
                    Sachgebiet = ordner.Sachgebiet,
                    Aktenzeichen = ordner.Aktenzeichen,
                    LfdNr = ordner.LfdNr,
                    Status = ordner.Status,
                    Final = ordner.Final,
                    ColorHex = ordner.ColorHex

                }).ToList();

                // -------------------------
                // STRING FILTERS
                // -------------------------

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    alleOrdner = alleOrdner
                     .Where(o => o.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase))
                     .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Type))
                {
                    alleOrdner = alleOrdner
                        .Where(o => o.Type != null && o.Type.Contains(search.Type, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.CreatorName))
                {
                    alleOrdner = alleOrdner
                        .Where(o => o.CreatorName != null && o.CreatorName.Contains(search.CreatorName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // -------------------------
                // DATE RANGE – CreationDate
                // -------------------------

                if (search.CreationDateVon.HasValue)
                {
                    alleOrdner = alleOrdner
                        .Where(o => o.CreationDate.Date >= search.CreationDateVon.Value.Date)
                        .ToList();
                }

                if (search.CreationDateBis.HasValue)
                {
                    alleOrdner = alleOrdner
                        .Where(o => o.CreationDate.Date <= search.CreationDateBis.Value.Date)
                        .ToList();

                }

                var sortMap = new Dictionary<string, Func<Ordner, object>>
                {
                    { "Name", o => o.Name },
                    { "CreationDate", o => o.CreationDate },
                    { "Type", o => o.Type },
                    { "CreatorName", o => o.CreatorName }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "Name";
                //}

                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "Name";
                }

                alleOrdner = sortDirection == "asc"
                    ? alleOrdner.OrderBy(sortMap[sortColumn]).ToList()
                    : alleOrdner.OrderByDescending(sortMap[sortColumn]).ToList();

                // -------------------------
                // PAGINATION
                // -------------------------

                var totalCount = alleOrdner.Count;

                var ordner = alleOrdner
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new OrdnerViewModel
                {
                    Vorgang = vorgang,
                    Ordner = ordner,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns
                };

                return PartialView("_OrdnerTable", model);

            }
            catch (OrdnerRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Ordners");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-unterordner")]
        public async Task<IActionResult> LoadUnterordner(OrdnerSearchModel search, Guid objectId, int page = 1, int pageSize = 10, string sortColumn = "Name", string sortDirection = "asc")
        {
            try
            {
                var columns = await _columnRepository.GetColumnsAsync("UnterOrdner", Uid);

                var ordnerDto = await SharedEAkte.GetOrdnerByObjektId(objektId: objectId);

                if (ordnerDto == null)
                {
                    return PartialView("_AccessDenied");
                }

                int accessResult;
                if (mode == "Mitzeichnung")
                {
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Ordner", sObjectId: ordnerDto.ObjektId, out _);
                }
                else
                {
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Ordner", sObjectId: ordnerDto.ObjektId, IsAdminModeActive(), out _);
                }

                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var ordner = new Ordner
                {
                    ObjektId = ordnerDto.ObjektId,
                    ParentObjectId = ordnerDto.ParentObjectId,
                    Name = ordnerDto.Name,
                    Type = ordnerDto.Type,
                    Bemerkungen = ordnerDto.Bemerkungen,
                    CreatorName = ordnerDto.CreatorName,
                    CreationDate = ordnerDto.CreationDate,
                    ModifyDate = ordnerDto.ModifyDate,
                    Modifier = ordnerDto.Modifier,
                    OwnerName = ordnerDto.OwnerName,
                    Hauptsachgebiet = ordnerDto.Hauptsachgebiet,
                    Sachgebiet = ordnerDto.Sachgebiet,
                    Aktenzeichen = ordnerDto.Aktenzeichen,
                    LfdNr = ordnerDto.LfdNr,
                    Status = ordnerDto.Status,
                    Final = ordnerDto.Final,
                    ColorHex = ordnerDto.ColorHex
                };

                var alleUnterordner = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: ordnerDto.ObjektId);

                var dokumente = await SharedEAkte.GetDokumenteByFolderId(folderId: ordnerDto.ObjektId);

                // -------------------------
                // MERGE FOLDERS + FILES
                // -------------------------

                var rows = new List<OrdnerDokumentRow>();

                rows.AddRange(alleUnterordner.Where(ordner => ordner.Type == "UnterOrdner").Select(o => new OrdnerDokumentRow
                {
                    ObjektId = o.ObjektId,
                    ParentObjektId = o.ParentObjectId,
                    Name = o.Name,
                    Type = o.Type,
                    Bemerkungen = o.Bemerkungen,
                    CreatorName = o.CreatorName,
                    CreationDate = o.CreationDate,
                    ModifyDate = o.ModifyDate,
                    Modifier = o.Modifier,
                    OwnerName = o.OwnerName,
                    Hauptsachgebiet = o.Hauptsachgebiet,
                    Sachgebiet = o.Sachgebiet,
                    Aktenzeichen = o.Aktenzeichen,
                    LfdNr = o.LfdNr,
                    Status = o.Status,
                    Final = o.Final,
                    ColorHex = o.ColorHex,
                    IsFolder = true
                }));

                rows.AddRange(dokumente.Select(d => new OrdnerDokumentRow
                {
                    ObjektId = d.ObjektId,
                    VersionId = d.Versionen.FirstOrDefault(v => v.Final)?.CurrentVersionId,
                    FileExtension = d.Versionen.FirstOrDefault(v => v.Final)?.FileExtension,
                    ParentObjektId = d.FolderId,
                    Name = d.Name,
                    Type = d.ContentType,
                    CreatorName = d.CreatorName,
                    CreationDate = d.CreationDate,
                    FileSize = d.Versionen.FirstOrDefault(v => v.IsActive)?.ContentSize,
                    ContentType = d.Versionen.FirstOrDefault(v => v.IsActive)?.ContentType,
                    FileExt = d.Versionen.FirstOrDefault(v => v.IsActive)?.FileExtension,
                    IsFolder = false,
                    IstAusVorlagenverwaltung = d.IstAusVorlagenverwaltung,
                    ModifyDate = d.Versionen.FirstOrDefault(v => v.IsActive)?.ModifiedDate,
                    Modifier = d.Versionen.FirstOrDefault(v => v.IsActive)?.ModifiedBy,
                    Version = d.Versionen.FirstOrDefault(v => v.IsActive)?.VersionNr,
                    Betreff = d.Subject,
                    LockedFrom = d.LockedFrom,
                    LockedAt = d.LockedAt,

                    Empfaenger = d.Empfaenger,
                    Absender = d.Absender
                }));

                // -------------------------
                // FILTER
                // -------------------------

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    rows = rows
                        .Where(o => o.Name != null && o.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Betreff))
                {
                    rows = rows
                        .Where(o => o.Betreff != null && o.Betreff.Contains(search.Betreff, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.CreatorName))
                {
                    rows = rows
                        .Where(o => o.CreatorName != null && o.CreatorName.Contains(search.CreatorName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Modifier))
                {
                    rows = rows
                        .Where(o => o.Modifier != null && o.Modifier.Contains(search.Modifier, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Version))
                {
                    rows = rows
                        .Where(o => o.Version != null && o.Version.Contains(search.Version, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (search.CreationDateVon.HasValue)
                {
                    rows = rows
                        .Where(o => o.CreationDate.HasValue && o.CreationDate.Value.Date >= search.CreationDateVon.Value.Date)
                        .ToList();
                }

                if (search.CreationDateBis.HasValue)
                {
                    rows = rows
                        .Where(o => o.CreationDate.HasValue && o.CreationDate.Value.Date <= search.CreationDateBis.Value.Date)
                        .ToList();
                }

                if (search.ModifyDateVon.HasValue)
                {
                    rows = rows
                        .Where(o => o.ModifyDate.HasValue && o.ModifyDate.Value.Date >= search.ModifyDateVon.Value.Date)
                        .ToList();
                }

                if (search.ModifyDateBis.HasValue)
                {
                    rows = rows
                        .Where(o => o.ModifyDate.HasValue && o.ModifyDate.Value.Date <= search.ModifyDateBis.Value.Date)
                        .ToList();
                }

                // -------------------------
                // SORT
                // -------------------------
                var sortMap = new Dictionary<string, Func<OrdnerDokumentRow, object>>
                {
                    { "Name", o => o.Name },
                    { "Betreff", o => o.Betreff },
                    { "CreationDate", o => o.CreationDate },
                    { "CreatorName", o => o.CreatorName },
                    { "ModifyDate", o => o.ModifyDate },
                    { "Modifier", o => o.Modifier },
                    { "Version", o => o.Version }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "CreationDate";
                //}

                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "Name";
                }

                rows = sortDirection == "asc"
                    ? rows.OrderBy(sortMap[sortColumn]).ToList()
                    : rows.OrderByDescending(sortMap[sortColumn]).ToList();

                // -------------------------
                // PAGINATION
                // -------------------------
                var totalCount = rows.Count;

                var pagedRows = rows
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // -------------------------
                // VIEWMODEL
                // -------------------------

                var model = new UnterOrdnerViewModel
                {
                    Rows = pagedRows,
                    Parentordner = ordner,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns
                };

                return PartialView("_UnterOrdnerTable", model);
            }
            catch (OrdnerRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Ordners");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-dokument")]
        public async Task<IActionResult> LoadDokument(Guid objectId)
        {
            try
            {
                // Spalten laden — dieselbe Tabelle wie in der Dokumentenübersicht (neu).
                var columns = await _columnRepository.GetColumnsAsync("Dokument", Uid);

                var dokument = await SharedEAkte.GetDokumenteByObjektId(objektId: objectId);

                if (dokument == null)
                {
                    return PartialView("_AccessDenied");
                }

                int accessResult;
                if (mode == "Mitzeichnung")
                {
                    accessResult = _restClass.CheckAccessMitzeichnung(Uid: Uid, NodeType: "Dokument", sObjectId: dokument.ObjektId, out _);
                }
                else
                {
                    accessResult = _restClass.CheckAccessSecondVersion(Uid: Uid, NodeType: "Dokument", sObjectId: dokument.ObjektId, IsAdminModeActive(), out _);
                }
                if (accessResult <= 0)
                {
                    return PartialView("_AccessDenied");
                }

                var aktiveVersion = dokument.Versionen.FirstOrDefault(v => v.IsActive);

                var model = new DokumentViewModel
                {
                    Dokument = new EAktePrototypProjektNewMvc.Models.Dokumente.Dokument
                    {
                        // Felder direkt aus Dokument
                        ObjektId = dokument.ObjektId,
                        Name = dokument.Name,
                        Type = dokument.Type,
                        Subject = dokument.Subject,
                        Aktenzeichen = dokument.Aktenzeichen,
                        Gegenstand = dokument.Gegenstand,
                        Antragsteller = dokument.Antragsteller,
                        ContentType = dokument.ContentType,
                        CreatorName = dokument.CreatorName,
                        CreationDate = dokument.CreationDate ?? DateTime.MinValue,
                        Modifier = dokument.Modifier,
                        ModifyDate = dokument.ModifyDate,
                        OwnerName = dokument.OwnerName,
                        VersionLabel = dokument.VersionLabel,
                        IsHidden = dokument.IsHidden,
                        IsDeleted = dokument.IsDeleted,
                        IsPublic = dokument.IsPublic,
                        IsSigned = dokument.IsSigned,
                        Final = dokument.Final,
                        Eingangsdatum = dokument.Eingangsdatum,
                        Ausgangsdatum = dokument.Ausgangsdatum,
                        LockedFrom = dokument.LockedFrom,
                        LockedAt = dokument.LockedAt,
                        FolderId = dokument.FolderId,
                        ACLName = dokument.ACLName,
                        IstAusVorlagenverwaltung = dokument.IstAusVorlagenverwaltung,

                        // Versionen komplett übernehmen
                        Versionen = dokument.Versionen?.Select(v => new DokumentVersion
                        {
                            VersionNr = v.VersionNr,
                            IsActive = v.IsActive,
                            IsArchived = v.IsArchived,
                            Description = v.Description,
                            CreatedDate = v.CreatedDate,
                            CreatedBy = v.CreatedBy,
                            ModifiedDate = v.ModifiedDate,
                            ModifiedBy = v.ModifiedBy,
                            Bemerkungen = v.Bemerkungen,
                            Dokumentdatum = v.Dokumentdatum,
                            DocumentObjektId = v.DocumentObjektId,
                            CurrentVersionId = v.CurrentVersionId,
                            ContentType = v.ContentType,
                            ContentSize = v.ContentSize,
                            FullContentSize = v.FullContentSize,
                            IsSigned = v.IsSigned,
                            Final = v.Final,
                            FileExtension = v.FileExtension,
                            // FileStream nur wenn wirklich nötig.
                            // FileStream = v.FileStream,
                        }).ToList() ?? new List<DokumentVersion>()
                    },
                    Columns = columns
                };

                return PartialView("_DokumentTable", model);
            }
            catch (DokumentRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Dokuments");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)   // neu
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-meine-vorgaenge")] // Meine Vorgänge.
        public async Task<IActionResult> LoadMeineVorgaenge(VorgangSearchModel search, Guid objectId, int page = 1, int pageSize = 10, string sortColumn = "Zulassungsnummer", string sortDirection = "asc")
        {
            try
            {
                // Spalten laden — dieselbe Tabelle wie bei den normalen Vorgängen (neu).
                var columns = await _columnRepository.GetColumnsAsync("Vorgang", Uid);

                var vorganengeDto = await SharedEAkte.GetAllVorgaenge();

                var alleVorgange = vorganengeDto.Select(vorgang => new Vorgang
                {
                    Aktenzeichen = vorgang.Aktenzeichen,
                    UserID = vorgang.UserID,
                    Abteilung = vorgang.Abteilung,
                    Stelle = vorgang.Stelle,
                    Zulassungsnummer = vorgang.Zulassungsnummer,

                    DatumAntrag = vorgang.DatumAntrag,
                    Antragsart = vorgang.Antragsart,
                    Bescheidtyp = vorgang.Bescheidtyp,

                    Gegenstand = vorgang.Gegenstand,
                    Status = vorgang.Status,
                    Betreff = vorgang.Betreff,
                    Bemerkung = vorgang.Bemerkung,

                    ObjektId = vorgang.ObjektId,

                    Aktenzustand = vorgang.Aktenzustand,
                    ErzeugtVon = vorgang.ErzeugtVon,
                    ErzeugtAm = vorgang.ErzeugtAm,

                    Veroeffentlicht = vorgang.Veroeffentlicht,

                    Icon = vorgang.Icon
                }).ToList();



                // Nur meine Vorgänge.
                alleVorgange = alleVorgange.Where(a =>
                    !string.IsNullOrWhiteSpace(a.UserID) &&
                    !string.IsNullOrWhiteSpace(Uid) &&
                    a.UserID.Trim() == Uid.Trim()
                ).ToList();


                if (!string.IsNullOrWhiteSpace(search.Aktenzeichen))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Aktenzeichen != null &&
                                    v.Aktenzeichen.Contains(search.Aktenzeichen, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Zulassungsnummer))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Zulassungsnummer != null &&
                                    v.Zulassungsnummer.Contains(search.Zulassungsnummer, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Status))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Status != null &&
                                    v.Status.Contains(search.Status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Aktenzustand))
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.Aktenzustand != null &&
                                    v.Aktenzustand.Contains(search.Aktenzustand, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // --------------------
                // DATE RANGE – DatumAntrag
                // --------------------

                if (search.DatumAntragVon.HasValue)
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.DatumAntrag.HasValue &&
                                    v.DatumAntrag.Value.Date >= search.DatumAntragVon.Value.Date)
                        .ToList();
                }

                if (search.DatumAntragBis.HasValue)
                {
                    alleVorgange = alleVorgange
                        .Where(v => v.DatumAntrag.HasValue &&
                                    v.DatumAntrag.Value.Date <= search.DatumAntragBis.Value.Date)
                        .ToList();
                }

                var sortMap = new Dictionary<string, Func<Vorgang, object>>
                {
                    { "Aktenzeichen", v => v.Aktenzeichen },
                    { "Zulassungsnummer", v => v.Zulassungsnummer },
                    { "Gegenstand", v => v.Gegenstand },
                    { "Status", v => v.Status },
                    { "DatumAntrag", v => v.DatumAntrag }
                };

                //if (!sortMap.ContainsKey(sortColumn))
                //{
                //    sortColumn = "DatumAntrag";
                //}

                // sortColumn: zusätzlich gegen sichtbare/sortierbare Spalten prüfen (neu).
                var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
                if (!sortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
                {
                    sortColumn = sortableKeys.FirstOrDefault(sortMap.ContainsKey) ?? "DatumAntrag";
                }

                alleVorgange = sortDirection == "asc"
                    ? alleVorgange.OrderBy(sortMap[sortColumn]).ToList()
                    : alleVorgange.OrderByDescending(sortMap[sortColumn]).ToList();

                var totalCount = alleVorgange.Count;

                var vorgange = alleVorgange
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new VorgangViewModel
                {
                    Vorgang = vorgange,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection,
                    Columns = columns,
                    ViewMode = "MeineVorgaenge"
                };

                return PartialView("_VorgangTable", model);
            }
            catch (VorgangRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Vorgangs");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-meine-dokumente")] // Meine Dokumente.
        public async Task<IActionResult> LoadMeineDokumente(DokumentSearchModel search, int page = 1, int pageSize = 10, string sortColumn = "CreationDate", string sortDirection = "desc")
        {
            try
            {
                // Spalten laden (neu, ganz am Anfang des try-Blocks).
                var columns = await _columnRepository.GetColumnsAsync("Dokument", Uid);

                var alleDokumenteRaw = await SharedEAkte.GetAllDokumenteMitVersionen();

                // Nur meine Dokumente.
                var myName = HttpContext.Session.GetString("UserName");

                var alleDokumente = alleDokumenteRaw
                    .Select(MapToListItem)
                    .Where(a => a.CreatorName == myName)
                    .ToList();

                var model = BuildListModel(alleDokumente, search, page, pageSize, sortColumn, sortDirection, "my", columns);

                return PartialView("_DokumentUebersicht", model);
            }
            catch (DokumentRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Dokuments");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-dokumente-deleted")] // Gelöschte Dokumente // Nur meine.
        public async Task<IActionResult> LoadDokumenteDeleted(DokumentSearchModel search, int page = 1, int pageSize = 10, string sortColumn = "CreationDate", string sortDirection = "desc")
        {
            try
            {
                // Spalten laden (neu, ganz am Anfang des try-Blocks).
                var columns = await _columnRepository.GetColumnsAsync("Dokument", Uid);

                var alleDokumenteRaw = await SharedEAkte.GetAllDokumenteDeleted();

                // Nur meine Dokumente.
                var myName = HttpContext.Session.GetString("UserName");

                var alleDokumente = alleDokumenteRaw
                    .Select(MapToListItem)
                    .Where(a => a.CreatorName == myName)
                    .ToList();

                var model = BuildListModel(alleDokumente, search, page, pageSize, sortColumn, sortDirection, "deleted", columns);

                return PartialView("_DokumentUebersicht", model);
            }
            catch (DokumentRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Dokuments");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-dokumentenuebersicht")]
        public async Task<IActionResult> LoadDokumentenuebersicht(DokumentSearchModel search, Guid? objectId, string? nodeType, int page = 1, int pageSize = 10, string sortColumn = "CreationDate", string sortDirection = "desc")
        {
            try
            {
                if (objectId == null || string.IsNullOrWhiteSpace(nodeType))
                {
                    return PartialView("_AccessDenied");
                }

                // Spalten laden (neu, ganz am Anfang des try-Blocks).
                var columns = await _columnRepository.GetColumnsAsync("Dokument", Uid);

                // Die Liste der rootFolderIds wird je nach nodeType gefüllt. Für "Akte" und "Vorgang" werden die entsprechenden Ordner ermittelt, für "Ordner" und "UnterOrdner" wird die ObjektId direkt verwendet.
                List<Guid> rootFolderIds = new List<Guid>();

                switch (nodeType)
                {
                    case "Akte":
                        {
                            var akte = await SharedEAkte.GetAkteByObjektId(objektId: objectId.Value);
                            if (akte == null)
                            {
                                return PartialView("_AccessDenied");
                            }

                            var vorgaenge = await SharedEAkte.GetVorgaengeByZulassungsnummer(zulassungsnummer: akte.Zulassungsnummer);
                            if (vorgaenge == null || !vorgaenge.Any())
                            {
                                return PartialView("_AccessDenied");
                            }

                            foreach (var vg in vorgaenge)
                            {
                                var ordnerAmVorgang = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: vg.ObjektId);
                                var rootOrdner = ordnerAmVorgang.Where(o => o.Type == "Ordner").ToList();
                                rootFolderIds.AddRange(rootOrdner.Select(o => o.ObjektId));
                            }

                            break;
                        }

                    case "Vorgang":
                        {
                            var vorgang = await SharedEAkte.GetVorgangByObjektId(objektId: objectId.Value);
                            if (vorgang == null)
                            {
                                return PartialView("_AccessDenied");
                            }

                            var ordnerAmVorgang = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: vorgang.ObjektId);
                            var rootOrdner = ordnerAmVorgang.Where(o => o.Type == "Ordner").ToList();
                            rootFolderIds.AddRange(rootOrdner.Select(o => o.ObjektId));

                            break;
                        }

                    case "Ordner":
                    case "UnterOrdner":
                        {
                            // Für Ordner und UnterOrdner wird die ObjektId direkt als rootFolderId verwendet.
                            rootFolderIds.Add(objectId.Value);
                            break;
                        }

                    default:
                        return PartialView("_AccessDenied");
                }

                if (!rootFolderIds.Any())
                {
                    return PartialView("_AccessDenied");
                }

                // Recursive alle FolderIds sammeln.
                var alleFolderIds = new List<Guid>();
                foreach (var rootId in rootFolderIds)
                {
                    alleFolderIds.AddRange(await GetAllFolderIdsRecursive(rootId));
                }
                alleFolderIds = alleFolderIds.Distinct().ToList();

                // Alle Dokumente aus allen FolderIds sammeln.
                var alleDokumenteRaw = new List<DreiF.Shared.EAkte.Dokumente.Dokument>();
                foreach (var folderId in alleFolderIds)
                {
                    var docsInFolder = await SharedEAkte.GetDokumenteByFolderId(folderId: folderId);
                    if (docsInFolder != null && docsInFolder.Any())
                    {
                        alleDokumenteRaw.AddRange(docsInFolder);
                    }
                }

                // Gruppieren nach ObjektId, um Duplikate zu entfernen (falls mehrere Versionen vorhanden sind).
                alleDokumenteRaw = alleDokumenteRaw
                    .GroupBy(d => d.ObjektId)
                    .Select(g => g.First())
                    .ToList();

                var alleDokumente = alleDokumenteRaw
                    .Select(MapToListItem)
                    .ToList();

                var model = BuildListModel(alleDokumente, search, page, pageSize, sortColumn, sortDirection, "overview", columns);
                model.ObjectId = objectId.Value;
                model.NodeType = nodeType;

                return PartialView("_DokumentUebersicht", model);
            }
            catch (DokumentRepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Dokumentenübersicht");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Tabellenspalten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();
                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        // Dokumente helpers. ###
        // Mappt ein Dokument auf ein flaches Listen-Element (aktive Version aufgelöst).
        private static DokumentListItem MapToListItem(DreiF.Shared.EAkte.Dokumente.Dokument d)
        {
            var version = d.Versionen.FirstOrDefault(v => v.IsActive)
                       ?? d.Versionen.LastOrDefault();

            return new DokumentListItem
            {
                ObjektId = d.ObjektId,
                Name = d.Name,
                ContentType = d.ContentType,
                CreatorName = d.CreatorName,
                CreationDate = d.CreationDate,
                ModifyDate = d.ModifyDate,
                Modifier = d.Modifier,
                VersionNr = version?.VersionNr,
                VersionId = version?.CurrentVersionId,
                LockedFrom = d.LockedFrom,

                Betreff = d.Subject,
                Version = version?.VersionNr,
                Subject = d.Subject,
                Gegenstand = d.Gegenstand,
                Antragsteller = d.Antragsteller,
                OwnerName = d.OwnerName,
                Aktenzeichen = d.Aktenzeichen,
                ACLName = d.ACLName,
                Eingangsdatum = d.Eingangsdatum,
                IsPublic = d.IsPublic,
                IsSigned = d.IsSigned,
                Final = d.Final,
                IsHidden = d.IsHidden,
                IsDeleted = d.IsDeleted,
                IstAusVorlagenverwaltung = d.IstAusVorlagenverwaltung,

                Description = version?.Description,
                Bemerkungen = version?.Bemerkungen,
                FileExtension = version?.FileExtension,
                ContentSize = version?.ContentSize ?? 0,
                VersionCreatedBy = version?.CreatedBy,
                Dokumentdatum = version?.Dokumentdatum
            };
        }

        private static bool ContainsIgnoreCase(string? source, string value) => source != null && source.Contains(value, StringComparison.OrdinalIgnoreCase);

        private static List<DokumentListItem> ApplyFilters(List<DokumentListItem> list, DokumentSearchModel? search)
        {
            if (search == null)
            {
                return list;
            }

            IEnumerable<DokumentListItem> q = list;

            if (!string.IsNullOrWhiteSpace(search.Name))
                q = q.Where(d => ContainsIgnoreCase(d.Name, search.Name));
            if (!string.IsNullOrWhiteSpace(search.Subject))
                q = q.Where(d => ContainsIgnoreCase(d.Subject, search.Subject));
            if (!string.IsNullOrWhiteSpace(search.CreatorName))
                q = q.Where(d => ContainsIgnoreCase(d.CreatorName, search.CreatorName));
            if (!string.IsNullOrWhiteSpace(search.Modifier))
                q = q.Where(d => ContainsIgnoreCase(d.Modifier, search.Modifier));
            if (!string.IsNullOrWhiteSpace(search.VersionNr))
                q = q.Where(d => ContainsIgnoreCase(d.VersionNr, search.VersionNr));
            if (!string.IsNullOrWhiteSpace(search.Aktenzeichen))
                q = q.Where(d => ContainsIgnoreCase(d.Aktenzeichen, search.Aktenzeichen));

            if (search.CreationDateVon.HasValue)
                q = q.Where(d => d.CreationDate.HasValue && d.CreationDate.Value.Date >= search.CreationDateVon.Value.Date);
            if (search.CreationDateBis.HasValue)
                q = q.Where(d => d.CreationDate.HasValue && d.CreationDate.Value.Date <= search.CreationDateBis.Value.Date);
            if (search.ModifyDateVon.HasValue)
                q = q.Where(d => d.ModifyDate.HasValue && d.ModifyDate.Value.Date >= search.ModifyDateVon.Value.Date);
            if (search.ModifyDateBis.HasValue)
                q = q.Where(d => d.ModifyDate.HasValue && d.ModifyDate.Value.Date <= search.ModifyDateBis.Value.Date);

            return q.ToList();
        }

        private static readonly Dictionary<string, Func<DokumentListItem, object>> DokumentSortMap = new()
        {
            { "Name",         d => d.Name ?? "" },
            { "Subject",      d => d.Subject ?? "" },
            { "CreationDate", d => (object)d.CreationDate },
            { "CreatorName",  d => d.CreatorName ?? "" },
            { "ModifyDate",   d => (object)(d.ModifyDate ?? DateTime.MinValue) },
            { "Modifier",     d => d.Modifier ?? "" },
            { "VersionNr",    d => (object)(d.VersionNr ?? "") },
            { "Aktenzeichen", d => d.Aktenzeichen ?? "" }
        };

        private static List<DokumentListItem> ApplySort(List<DokumentListItem> list, ref string sortColumn, string sortDirection, List<TableColumnDto> columns)
        {
            // sortColumn muss existieren UND laut Spalten-Konfiguration sortierbar/sichtbar sein (neu).
            var sortableKeys = columns.Where(c => c.IsSortable).Select(c => c.ColumnKey).ToHashSet();
            if (!DokumentSortMap.ContainsKey(sortColumn) || !sortableKeys.Contains(sortColumn))
            {
                sortColumn = sortableKeys.FirstOrDefault(DokumentSortMap.ContainsKey) ?? "CreationDate";
            }

            return sortDirection == "asc"
                ? list.OrderBy(DokumentSortMap[sortColumn]).ToList()
                : list.OrderByDescending(DokumentSortMap[sortColumn]).ToList();
        }

        // Filter + Sort + Pagination - fertiges ViewModel.
        private static DokumentListViewModel BuildListModel(List<DokumentListItem> alleDokumente, DokumentSearchModel? search, int page, int pageSize, string sortColumn, string sortDirection, string table, List<TableColumnDto> columns)
        {
            alleDokumente = ApplyFilters(alleDokumente, search);
            alleDokumente = ApplySort(alleDokumente, ref sortColumn, sortDirection, columns);   // geändert

            return new DokumentListViewModel
            {
                Dokumente = alleDokumente
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                Search = search ?? new DokumentSearchModel(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = alleDokumente.Count,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
                Table = table,
                Columns = columns   // neu
            };
        }

        // Sammelt rekursiv alle Ordner-IDs, beginnend mit dem angegebenen Ordner.
        private async Task<List<Guid>> GetAllFolderIdsRecursive(Guid folderId)
        {
            var result = new List<Guid> { folderId };

            var children = await SharedEAkte.GetOrdnerByParentObjectId(parentObjectId: folderId);
            var unterOrdner = children.Where(o => o.Type == "UnterOrdner").ToList();

            foreach (var sub in unterOrdner)
            {
                var subIds = await GetAllFolderIdsRecursive(sub.ObjektId);
                result.AddRange(subIds);
            }

            return result;
        }
        // Dokumente helpers. ###



        // Baum Aktionen.
        [HttpPost("delete-dokument")]
        public IActionResult DeleteDokument([FromBody] DokumentRequest request)
        {
            if (request?.ObjectIds == null || request.ObjectIds.Count == 0 || request.ObjectIds.Any(id => id == Guid.Empty))
            {
                return BadRequest();
            }

            try
            {
                var skippedLocked = _eAkteRepo.DeleteDokumente(request.ObjectIds);

                if (skippedLocked.Count > 0)
                {
                    var allSkipped = skippedLocked.Count == request.ObjectIds.Count;
                    return StatusCode(409, new
                    {
                        message = allSkipped
                            ? "Alle ausgewählten Dokumente werden aktuell bearbeitet und konnten nicht gelöscht werden."
                            : $"{skippedLocked.Count} von {request.ObjectIds.Count} Dokumenten werden aktuell bearbeitet und wurden nicht gelöscht.",
                        skippedObjectIds = skippedLocked
                    });
                }

                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen der Dokumente");
                return StatusCode(500);
            }
        }

        [HttpPost("restore-dokument")]
        public IActionResult RestoreDokument([FromBody] DokumentRequest request)
        {
            if (request?.ObjectIds == null || request.ObjectIds.Count == 0 || request.ObjectIds.Any(id => id == Guid.Empty))
            {
                return BadRequest();
            }

            try
            {
                _eAkteRepo.RestoreDokumente(request.ObjectIds);
                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Wiederherstellen der Dokumente");
                return StatusCode(500);
            }
        }

        [HttpPost("purge-dokument")]
        public IActionResult PurgeDokument([FromBody] DokumentRequest request)
        {
            if (request?.ObjectIds == null || request.ObjectIds.Count == 0 || request.ObjectIds.Any(id => id == Guid.Empty))
            {
                return BadRequest();
            }

            try
            {
                _eAkteRepo.PurgeDokumente(request.ObjectIds);
                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim endgültigen Löschen der Dokumente");
                return StatusCode(500);
            }
        }

        [HttpPost("delete-ordner")]
        public IActionResult DeleteOrdner(Guid objectId)
        {
            try
            {
                if (objectId == Guid.Empty) return BadRequest();

                _eAkteRepo.DeleteOrdner(objectId);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                // Child folders exist — caller must delete them first.
                return Conflict(new { message = ex.Message });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Ordners");
                return StatusCode(500);
            }
        }

        [HttpPost("rename-ordner")]
        public IActionResult RenameOrdner(Guid objectId, string name)
        {
            try
            {
                if (objectId == Guid.Empty || string.IsNullOrWhiteSpace(name)) return BadRequest();

                _eAkteRepo.RenameOrdner(objectId, name);
                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Umbenennen des Ordners");
                return StatusCode(500);
            }
        }

        [HttpPost("upload-icon")]
        public IActionResult UploadIcon(Guid ObjektId, string NodeType, IFormFile Icon)
        {
            try
            {
                if (!IsAdminModeActive())
                {
                    return Forbid();
                }

                var rollen = HttpContext.Session.GetString("Rollen");
                if (string.IsNullOrWhiteSpace(rollen) || !rollen.Split(',').Contains("Administrator")) return Forbid();

                if (Icon == null || Icon.Length == 0) return BadRequest("No file");

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    Icon.CopyTo(ms);
                    bytes = ms.ToArray();
                }
                _eAkteRepo.UpdateIcon(ObjektId, NodeType, bytes);
                return Ok(new { iconBase64 = Convert.ToBase64String(bytes) });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Fehler beim Hochladen des Icons");
                return BadRequest("Unknown NodeType");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Hochladen des Icons");
                return StatusCode(500);
            }
        }

        [HttpPost("delete-icon")]
        public IActionResult DeleteIcon(Guid ObjektId, string NodeType)
        {
            try
            {
                if (!IsAdminModeActive())
                {
                    return Forbid();
                }
                var rollen = HttpContext.Session.GetString("Rollen");
                if (string.IsNullOrWhiteSpace(rollen) || !rollen.Split(',').Contains("Administrator")) return Forbid();

                _eAkteRepo.DeleteIcon(ObjektId, NodeType);
                return Ok(new { iconBase64 = (string)null });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Icons");
                return BadRequest("Unknown NodeType");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Icons");
                return StatusCode(500);
            }
        }




        // Ordner.    
        [HttpGet("get-modal-data-ordner")]
        public async Task<IActionResult> GetModalDataOrdner(string? ObjectId, string mode)
        {
            try
            {
                if (!Guid.TryParse(ObjectId, out var objektId)) return BadRequest("Invalid ObjectId");

                var ordnerParent = await SharedEAkte.GetOrdnerByObjektId(objektId: objektId);

                if (ordnerParent == null) return NotFound();

                ViewBag.Mode = mode;
                ViewBag.ParentObjectId = ordnerParent.ObjektId;
                ViewBag.Aktenzeichen = ordnerParent.Aktenzeichen;
                ViewBag.ParentName = ordnerParent.Name;
                ViewBag.ParentColorHex = ordnerParent.ColorHex;
                if (mode == "edit")
                {
                    ViewBag.Name = ordnerParent.Name;
                    ViewBag.Bemerkung = ordnerParent.Bemerkungen;
                    ViewBag.ColorHex = ordnerParent.ColorHex;
                }
                return PartialView("_CreateOrdner");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Ordner-Modals");
                return StatusCode(500);
            }
        }

        [HttpPost("create-ordner")]
        public async Task<IActionResult> CreateOrdner([FromBody] Ordner ordner)
        {
            try
            {
                var parentOrdner = await SharedEAkte.GetOrdnerByObjektId(ordner.ParentObjectId.Value);

                if (parentOrdner == null) return NotFound("ParentOrdner nicht gefunden.");

                ordner.CreationDate = DateTime.Now;
                ordner.CreatorName = HttpContext.Session.GetString("UserName");
                ordner.Modifier = HttpContext.Session.GetString("UserName");
                ordner.ModifyDate = DateTime.Now;
                ordner.IsDeleted = false;
                ordner.IsHidden = false;
                ordner.IsPublic = false;
                ordner.IsSigned = false;
                ordner.Final = false;
                ordner.Aktenzeichen = parentOrdner.Aktenzeichen;
                ordner.LfdNr = parentOrdner.LfdNr;
                ordner.Hauptsachgebiet = parentOrdner.Hauptsachgebiet;
                ordner.Sachgebiet = parentOrdner.Sachgebiet;
                ordner.OwnerName = parentOrdner.OwnerName;
                ordner.OwnerPermit = parentOrdner.OwnerPermit;
                ordner.GroupPermit = parentOrdner.GroupPermit;
                ordner.WorldPermit = parentOrdner.WorldPermit;
                ordner.ACLName = null;
                ordner.ACLDomain = null;
                ordner.Type = "UnterOrdner";
                ordner.Status = parentOrdner.Status;
                ordner.Path = parentOrdner.Path != null ? $"{parentOrdner.Path}/{ordner.Name}" : ordner.Name;
                _eAkteRepo.CreateOrdner(ordner);
                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des Ordners");
                return Json(new { success = false });
            }
        }

        [HttpPost("update-ordner")]
        public async Task<IActionResult> UpdateOrdner([FromBody] Ordner ordner)
        {
            try
            {
                ordner.Modifier = HttpContext.Session.GetString("UserName");
                ordner.ModifyDate = DateTime.Now;
                _eAkteRepo.UpdateOrdner(ordner);
                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Ordners");
                return Json(new { success = false });
            }
        }




        // Dokument. 
        [HttpGet("get-modal-data-dokument")]
        public async Task<IActionResult> GetModalDataDokument(string? ObjectId, string mode)
        {
            try
            {
                if (!Guid.TryParse(ObjectId, out var objektId)) return BadRequest("Invalid ObjectId");

                if (mode == "create")
                {
                    var ordnerParent = await SharedEAkte.GetOrdnerByObjektId(objektId: objektId);

                    if (ordnerParent == null) return NotFound();

                    ViewBag.Mode = mode;
                    ViewBag.ParentObjectId = ordnerParent.ObjektId;
                    ViewBag.Aktenzeichen = ordnerParent.Aktenzeichen;
                    ViewBag.ParentName = ordnerParent.Name;
                    ViewBag.ParentColorHex = ordnerParent.ColorHex;
                }

                if (mode == "edit")
                {
                    var dok = await SharedEAkte.GetDokumenteByObjektId(objektId: objektId);

                    if (dok == null) return NotFound();

                    var ordnerParent = await SharedEAkte.GetOrdnerByObjektId(dok.FolderId);
                    ViewBag.Mode = mode;
                    ViewBag.ParentObjectId = dok.ObjektId;
                    ViewBag.Aktenzeichen = dok.Aktenzeichen;
                    ViewBag.ParentName = ordnerParent?.Name;
                    ViewBag.ParentColorHex = ordnerParent?.ColorHex;
                }

                return PartialView("_CreateDokument");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Dokument-Modals");
                return StatusCode(500);
            }
        }

        [HttpPost("create-dokument")]
        public async Task<IActionResult> CreateDokument([FromForm] Guid parentObjectId, [FromForm] IFormFile file, [FromForm] string? subject, [FromForm] string? gegenstand, [FromForm] string? description, [FromForm] string? bemerkungen)
        {
            try
            {
                if (file == null || file.Length == 0) return Json(new { success = false, message = "Keine Datei übergeben." });
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }
                var parentObjekt = await SharedEAkte.GetOrdnerByObjektId(parentObjectId);
                var fileExtension = Path.GetExtension(file.FileName);
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var currentUserName = HttpContext.Session.GetString("UserName") ?? "System";

                // Fallback für ContentType, falls der Browser (z.B. bei Outlook-Drag&Drop
                // von .eml/.msg-Dateien) keinen oder einen leeren MIME-Type mitliefert.
                var contentType = file.ContentType;
                if (string.IsNullOrWhiteSpace(contentType) || contentType == "application/octet-stream")
                {
                    contentType = fileExtension.ToLowerInvariant() switch
                    {
                        ".eml" => "message/rfc822",
                        ".msg" => "application/vnd.ms-outlook",
                        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                        ".txt" => "text/plain",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        _ => "application/octet-stream"
                    };
                }

                // Email-Metadaten (Subject, Absender, Empfänger) haben Vorrang vor den übergebenen Parametern.
                var emailMeta = EmailMetadataExtractor.TryExtract(fileBytes, fileExtension);

                var finalSubject = !string.IsNullOrWhiteSpace(emailMeta?.Subject)
                    ? emailMeta.Subject
                    : (string.IsNullOrWhiteSpace(subject) ? null : subject.Trim());

                var finalAbsender = emailMeta?.Absender;
                var finalEmpfaenger = emailMeta?.Empfaenger;

                // Nummer generieren: nnnn.yy, jährlich zurückgesetzt.
                var currentYearSuffix = DateTime.Now.ToString("yy");
                var alleDokumente = await SharedEAkte.GetAllDokumente();
                var maxNumber = alleDokumente
                    .Select(d => d.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n) && n.EndsWith("." + currentYearSuffix))
                    .Select(n => n.Substring(0, n.Length - (currentYearSuffix.Length + 1)))
                    .Select(n => long.TryParse(n, out var num) ? num : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                var nextNumber = maxNumber + 1;
                var generatedName = $"{nextNumber}.{currentYearSuffix}";
                var dokument = new Dokument
                {
                    ObjektId = Guid.Empty,
                    FolderId = parentObjectId,
                    Aktenzeichen = parentObjekt?.Aktenzeichen,
                    Name = generatedName,
                    ContentType = contentType,
                    CreatorName = currentUserName,
                    CreationDate = DateTime.Now,
                    IsHidden = false,
                    IsDeleted = false,
                    IsPublic = false,
                    IsSigned = false,
                    Final = true,
                    IstAusVorlagenverwaltung = false,
                    Subject = finalSubject,
                    Gegenstand = string.IsNullOrWhiteSpace(gegenstand) ? null : gegenstand.Trim(),
                    OwnerName = currentUserName,
                    VersionLabel = "1.0",
                    Antragsteller = currentUserName,
                    Eingangsdatum = DateTime.Now,
                    Empfaenger = finalEmpfaenger,
                    Absender = finalAbsender,
                    Versionen = new List<DokumentVersion>
                {
                    new DokumentVersion
                    {
                        CurrentVersionId = Guid.Empty,
                        DocumentObjektId = Guid.Empty,
                        VersionNr        = "1.0",
                        IsActive         = true,
                        IsArchived       = false,
                        CreatedDate      = DateTime.Now,
                        CreatedBy        = currentUserName,
                        FileStream       = fileBytes,
                        ContentType      = contentType,
                        ContentSize      = file.Length,
                        FullContentSize  = file.Length,
                        FileExtension    = fileExtension,
                        IsSigned         = false,
                        Final            = true,
                        Description      = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                        Bemerkungen      = string.IsNullOrWhiteSpace(bemerkungen) ? null : bemerkungen.Trim()
                    }
                }
                };
                dokument.Versionen[0].DocumentObjektId = dokument.ObjektId;
                _eAkteRepo.CreateDokument(dokument);
                return Json(new { success = true, name = generatedName });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des Dokuments");
                return Json(new { success = false, message = "Fehler beim Erstellen des Dokuments." });
            }
        }

        [HttpPost("update-dokument")]
        public async Task<IActionResult> UpdateDokument([FromForm] Guid parentObjectId, [FromForm] IFormFile file, [FromForm] string? description, [FromForm] string? bemerkungen)
        {
            try
            {
                if (file == null || file.Length == 0) return Json(new { success = false, message = "Keine Datei übergeben." });

                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                var fileExtension = Path.GetExtension(file.FileName);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
                var currentUserName = HttpContext.Session.GetString("UserName") ?? "System";

                var newVersion = new DokumentVersion
                {
                    CurrentVersionId = Guid.Empty,
                    DocumentObjektId = parentObjectId,
                    VersionNr = null,
                    IsActive = true,
                    IsArchived = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = currentUserName,
                    FileStream = fileBytes,
                    ContentType = file.ContentType,
                    ContentSize = file.Length,
                    FullContentSize = file.Length,
                    FileExtension = fileExtension,
                    IsSigned = false,
                    Final = true,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    Bemerkungen = string.IsNullOrWhiteSpace(bemerkungen) ? null : bemerkungen.Trim()
                };

                _eAkteRepo.UpdateDokument(
                    dokumentObjektId: parentObjectId,
                    name: nameWithoutExt,
                    contentType: file.ContentType,
                    newVersion: newVersion,
                    modifierName: currentUserName);

                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Dokuments");
                return Json(new { success = false, message = "Fehler beim Aktualisieren des Dokuments." });
            }
        }

        [HttpGet("download-dokument")]
        public IActionResult DownloadDokument(Guid versionId)
        {
            try
            {
                if (versionId == Guid.Empty) return BadRequest();

                var version = _eAkteRepo.GetVersionByVersionId(versionId);
                if (version?.FileStream == null) return NotFound();

                var fileName = $"{version.DokumentName ?? "download"}{version.FileExtension}";

                // MIME-Type anhand der Dateiendung ermitteln.
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fileName, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return File(version.FileStream, contentType, fileName);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Herunterladen der Version {VersionId}", versionId);
                return StatusCode(500);
            }
        }

        [HttpPost("start-view")] // Öffnen eines Dokuments nur zum Ansehen (read-only) via Office-Protokoll.
        public IActionResult StartView([FromForm] Guid versionId)
        {
            var version = _eAkteRepo.GetVersionByVersionId(versionId);
            if (version == null) return NotFound();

            var currentUserName = HttpContext.Session.GetString("UId") ?? "System";

            var token = Guid.NewGuid();
            var fileName = $"{version.DokumentName}{version.FileExtension}";

            // Gleiche Session-Struktur wie beim Edit, damit die Middleware unverändert bleibt.
            // Kürzere Lebensdauer reicht hier, da nur angesehen wird.
            _editSessionRepository.Create(new DokumentEditSession
            {
                TokenId = token,
                DocumentObjektId = version.DocumentObjektId,
                OriginalVersionId = versionId,
                UserId = currentUserName,
                FileName = fileName,
                ContentType = version.ContentType,
                IssuedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(30),
                MandantId = _mandantProvider.MandantId,
                IsViewOnly = true
            });

            var editUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/webdav/dokumente/{token}/{Uri.EscapeDataString(fileName)}";

            return Json(new { success = true, editUrl, token, contentType = version.FileExtension });
        }

        [HttpPost("create-converter-session")] //PDF-Konverter Session.
        public async Task<IActionResult> CreateConverterSession([FromQuery] Guid objectId, [FromQuery] string? mode = null)
        {
            if (objectId == Guid.Empty)
            {
                return BadRequest(new { error = "ObjectId not found" });
            }


            if (string.IsNullOrEmpty(Uid))
            {
                return Unauthorized(new { error = "UserId not found — Sitzung möglicherweise abgelaufen" });
            }

            if (mode != null && mode != "buttons" && mode != "table")
            {
                return BadRequest(new { error = "Invalid mode. Must be 'buttons' or 'table'." });
            }

            if (mode == "table")
            {
                // TODO: Berechtigung.
                var dok = await SharedEAkte.GetDokumenteByObjektId(objectId);

                var session = _pdfConverterSessionRepository.CreateSession(objektId: objectId, userId: Uid, lifetime: TimeSpan.FromMinutes(10), name: dok.Name);

                return Ok(new { token = session.TokenId });
            }
            else
            {
                var ordner = await SharedEAkte.GetOrdnerByObjektId(objectId);

                var rightorder = await SharedEAkte.GetOrdnerByAktenzeichen(ordner.Aktenzeichen);

                var anlagen = rightorder.FirstOrDefault(o => o.Name == "Anlagen");

                var dok = (await SharedEAkte.GetDokumenteByFolderId(anlagen.ObjektId)).FirstOrDefault();

                var session = _pdfConverterSessionRepository.CreateSession(objektId: dok.ObjektId, userId: Uid, lifetime: TimeSpan.FromMinutes(10), name: dok.Name);

                return Ok(new { token = session.TokenId });
            }
        }

        // Wiedervorlage erstellen und als ICS-Datei zurückgeben.
        [HttpPost("create-wiedervorlage")]
        public IActionResult CreateWiedervorlage([FromBody] Wiedervorlage dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.ObjectId))
            {
                return BadRequest();
            }

            if (!DateTime.TryParse($"{dto.Date} {dto.Time}", out var start))
            {
                return BadRequest("Ungültiges Datum");
            }

            // ICS-Datei: Startzeit + 30 Minuten für Endzeit.
            var end = start.AddMinutes(30);

            // Link auf das Objekt im Projekt.
            var link = $"{Request.Scheme}://{Request.Host}/bescheidbearbeitung?ObjectId={Uri.EscapeDataString(dto.ObjectId)}";

            // ICS-Datei: Zusammenfassung.
            var summary = $"Wiedervorlage: {dto.Title}";

            // ICS-Datei erstellen.
            var ics = WiedervorlageHelper.BuildIcs(summary, dto.Remark, link, start, end);

            var bytes = new UTF8Encoding(false).GetBytes(ics);
            return File(bytes, "text/calendar; charset=utf-8", "Wiedervorlage.ics");
        }

        [HttpGet("create-email-draft")]
        public async Task<IActionResult> CreateEmailDraft(Guid objectId)
        {
            try
            {
                //if (versionId == Guid.Empty) return BadRequest();

                var version = _eAkteRepo.GetLatestVersionByDocumentObjektId(objectId);
                if (version?.FileStream == null) return NotFound();

                var attachmentName = $"{version.DokumentName ?? "download"}{version.FileExtension}";
                var documentUrl = $"{Request.Scheme}://{Request.Host}/bescheidbearbeitung?ObjectId={version.DocumentObjektId}";

                //var fromAddress = HttpContext.Session.GetString("UserEmail") ?? "noreply@platzhalter.local";
                var fromAddress = "IhreE-Mail-Adresse-wird-beim-Versandautomatisch-eingetragen@platzhalter.local";

                var message = new MimeMessage();
                message.Subject = version.DokumentName ?? "Dokument";
                message.From.Add(MailboxAddress.Parse(fromAddress));

                var builder = new BodyBuilder
                {
                    TextBody = $"Anbei das Dokument \"{attachmentName}\".\n\nLink zum Dokument:\n{documentUrl}"
                };

                // Dokument als Anhang hinzufügen.
                builder.Attachments.Add(attachmentName, version.FileStream);

                message.Body = builder.ToMessageBody();

                // Damit der E-Mail-Client den Entwurf als Entwurf öffnet.
                message.Headers.Add("X-Unsent", "1");

                var outStream = new MemoryStream();
                message.WriteTo(outStream);
                outStream.Position = 0;

                return File(outStream, "message/rfc822", "Dokument.eml");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des E-Mail-Entwurfs für Version {ObjektId}", objectId);
                return StatusCode(500);
            }
        }

        // Erstellt einen E-Mail-Entwurf mit PDF-Anhang basierend auf einer PDF-Konverter-Session.
        [TokenAuthorize(TokenSessionSource.PdfConverter)]
        [HttpPost("create-email-draft-pdf")]
        public IActionResult CreateEmailDraftPdf([FromBody] EmailPdfDraftRequest request)
        {
            try
            {
                if (request == null || request.Token == Guid.Empty)
                {
                    return BadRequest();
                }

                var session = _pdfConverterSessionRepository.GetByToken(request.Token);
                if (session == null)
                {
                    return NotFound("Ungültiger oder abgelaufener Token.");
                }

                byte[]? pdfBytes;
                try
                {
                    pdfBytes = _pdfConverterSessionRepository.GetStagedFileStream(request.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Lesen des StagedFileStream für Token {Token}", request.Token);
                    return StatusCode(500);
                }

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return NotFound("Kein PDF für diesen Token gefunden.");
                }

                // Titel serverseitig ermitteln (bisher kam er als Filename vom Client)
                var attachmentName = $"{session.Name}.pdf";

                var documentUrl = $"{Request.Scheme}://{Request.Host}/bescheidbearbeitung?ObjectId={session.DocumentObjektId}";
                //var fromAddress = HttpContext.Session.GetString("UserEmail") ?? "noreply@platzhalter.local";
                var fromAddress = "IhreE-Mail-Adresse-wird-beim-Versandautomatisch-eingetragen@platzhalter.local";

                var message = new MimeMessage();
                message.Subject = attachmentName;
                message.From.Add(MailboxAddress.Parse(fromAddress));

                var builder = new BodyBuilder
                {
                    TextBody = $"Anbei das Dokument \"{attachmentName}\".\n\nLink zum Dokument:\n{documentUrl}"
                };

                builder.Attachments.Add(attachmentName, pdfBytes);
                message.Body = builder.ToMessageBody();

                // Damit der E-Mail-Client den Entwurf als Entwurf öffnet.
                message.Headers.Add("X-Unsent", "1");

                var outStream = new MemoryStream();
                message.WriteTo(outStream);
                outStream.Position = 0;

                return File(outStream, "message/rfc822", "Dokument.eml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des E-Mail-Entwurfs mit PDF-Anhang für Token {Token}", request?.Token);
                return StatusCode(500);
            }
        }




        // Word Bearbeitung.
        [HttpPost("start-edit")]
        public IActionResult StartEdit([FromForm] Guid versionId)
        {
            var version = _eAkteRepo.GetVersionByVersionId(versionId);
            if (version == null) return NotFound();

            var currentUserName = HttpContext.Session.GetString("UId") ?? "System";

            // Prüfen, ob das Dokument bereits von einem anderen Benutzer gesperrt ist.
            if (_eAkteRepo.IsLockedByOther(version.DocumentObjektId, currentUserName, out var lockedBy))
            {
                return Json(new { success = false, message = $"Dokument wird bereits von {lockedBy} bearbeitet." });
            }

            // Sperrzeit aus Config laden, Fallback auf 24 Stunden.
            var config = _configRepo.LoadConfig();
            int lockHours = config.DokumentEditLockStunden ?? 24;
            Console.WriteLine($"DokumentEditLockStunden aus Config: {config.DokumentEditLockStunden}");

            var token = Guid.NewGuid();
            var fileName = $"{version.DokumentName}{version.FileExtension}";

            _editSessionRepository.Create(new DokumentEditSession
            {
                TokenId = token,
                DocumentObjektId = version.DocumentObjektId,
                OriginalVersionId = versionId,
                UserId = currentUserName,
                FileName = fileName,
                ContentType = version.ContentType,
                IssuedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddHours(lockHours),
                MandantId = _mandantProvider.MandantId,
                IsViewOnly = false
            });

            // Dokument sperren, damit andere Benutzer nicht gleichzeitig bearbeiten können.
            _eAkteRepo.LockDokument(version.DocumentObjektId, currentUserName, DateTime.Now);

            var editUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/webdav/dokumente/{token}/{Uri.EscapeDataString(fileName)}";

            return Json(new { success = true, editUrl, token, contentType = version.FileExtension });
        }

        [HttpPost("confirm-edit")] // Bestätigen der Bearbeitung und Übernahme der Änderungen (neue Version).
        public IActionResult ConfirmEdit([FromForm] Guid token)
        {
            var session = _editSessionRepository.GetByToken(token);
            if (session == null)
            {
                return Json(new { success = false, message = "Sitzung nicht gefunden oder abgelaufen." });
            }

            var currentUserName = HttpContext.Session.GetString("UId") ?? "System";

            // Nur der Besitzer der Sitzung oder ein Administrator im aktiven Admin-Modus darf die Sitzung verwalten.
            if (!string.Equals(session.UserId, currentUserName, StringComparison.OrdinalIgnoreCase) && !IsAdminModeActive())
            {
                return Json(new { success = false, message = "Keine Berechtigung für diese Sitzung." });
            }

            if (!session.HasPendingChanges || session.StagedFileStream == null)
            {
                return Json(new { success = false, message = "Keine Änderungen zum Übernehmen vorhanden." });
            }

            var newVersion = new DokumentVersion
            {
                CurrentVersionId = Guid.Empty,
                DocumentObjektId = session.DocumentObjektId,
                VersionNr = null,
                IsActive = true,
                IsArchived = false,
                CreatedDate = DateTime.Now,
                CreatedBy = currentUserName,
                FileStream = session.StagedFileStream,
                ContentType = session.ContentType,
                ContentSize = session.StagedFileStream.Length,
                FullContentSize = session.StagedFileStream.Length,
                FileExtension = Path.GetExtension(session.FileName),
                IsSigned = false,
                Final = true
            };

            try
            {
                _eAkteRepo.UpdateDokument(
                    dokumentObjektId: session.DocumentObjektId,
                    name: Path.GetFileNameWithoutExtension(session.FileName),
                    contentType: session.ContentType,
                    newVersion: newVersion,
                    modifierName: currentUserName);

                _editSessionRepository.Delete(token);
                _eAkteRepo.UnlockDokument(session.DocumentObjektId);
                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Übernehmen der Word-Bearbeitung");
                return Json(new { success = false, message = "Fehler beim Speichern der Version." });
            }
        }

        [HttpPost("confirm-edit-same-version")] // Bestätigen der Bearbeitung und Übernahme der Änderungen in derselben Version.
        public IActionResult ConfirmEditSameVersion([FromForm] Guid token)
        {
            var session = _editSessionRepository.GetByToken(token);
            if (session == null)
            {
                return Json(new { success = false, message = "Sitzung nicht gefunden oder abgelaufen." });
            }

            var currentUserName = HttpContext.Session.GetString("UId") ?? "System";

            // Nur der Besitzer der Sitzung oder ein Administrator im aktiven Admin-Modus darf die Sitzung verwalten.
            if (!string.Equals(session.UserId, currentUserName, StringComparison.OrdinalIgnoreCase) && !IsAdminModeActive())
            {
                return Json(new { success = false, message = "Keine Berechtigung für diese Sitzung." });
            }

            if (!session.HasPendingChanges || session.StagedFileStream == null)
            {
                return Json(new { success = false, message = "Keine Änderungen zum Übernehmen vorhanden." });
            }

            try
            {
                _eAkteRepo.UpdateDokumentSameVersion(
                    dokumentObjektId: session.DocumentObjektId,
                    fileStream: session.StagedFileStream,
                    contentType: session.ContentType,
                    modifierName: currentUserName);

                _editSessionRepository.Delete(token);
                _eAkteRepo.UnlockDokument(session.DocumentObjektId);
                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Übernehmen der Word-Bearbeitung in derselben Version");
                return Json(new { success = false, message = "Fehler beim Speichern der Version." });
            }
        }

        [HttpPost("skip-edit")] // Abbrechen der Bearbeitung.
        public IActionResult SkipEdit([FromForm] Guid token)
        {
            var session = _editSessionRepository.GetByToken(token);
            if (session == null)
            {
                // Sitzung existiert nicht mehr.
                return Json(new { success = true });
            }

            var currentUserName = HttpContext.Session.GetString("UId") ?? "System";
            // Nur der Besitzer der Sitzung oder ein Administrator im aktiven Admin-Modus darf die Sitzung verwalten.
            if (!string.Equals(session.UserId, currentUserName, StringComparison.OrdinalIgnoreCase)
                && !IsAdminModeActive())
            {
                return Json(new { success = false, message = "Keine Berechtigung für diese Sitzung." });
            }

            _editSessionRepository.Delete(token);
            _eAkteRepo.UnlockDokument(session.DocumentObjektId);
            return Json(new { success = true });
        }

        // Aktive Edit-Sitzung für ein Dokument abrufen (nur für den Besitzer der Sitzung oder Administrator im aktiven Admin-Modus).
        [HttpGet("my-edit-session")]
        public IActionResult MyEditSession(Guid objectId)
        {
            var currentUserName = HttpContext.Session.GetString("UId") ?? "System";

            var session = _editSessionRepository.GetActiveByDocumentObjektId(objectId);
            if (session == null || session.IsViewOnly)
            {
                return Json(new { found = false });
            }

            // Administrator im aktiven Admin-Modus darf ebenfalls auf die Sitzung zugreifen.
            bool isAdminModeActive = IsAdminModeActive();

            // Fremde Sitzung — Token nicht preisgeben.
            if (!isAdminModeActive && !string.Equals(session.UserId, currentUserName, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { found = false });
            }

            return Json(new
            {
                found = true,
                token = session.TokenId,
                versionId = session.OriginalVersionId,
                hasPendingChanges = session.HasPendingChanges
            });
        }

        // Word Bearbeitung plugin.
        [TokenAuthorize(TokenSessionSource.DokumentEdit)]
        [HttpPost("confirm-edit-newVersion")]
        public IActionResult ConfirmEditNewVersion([FromBody] ConfirmEditRequestDto request)
        {
            Console.WriteLine($"ConfirmEditNewVersion aufgerufen: Token={request?.Token}, DocumentBytesLength={request?.DocumentBytes?.Length ?? 0}");

            var token = request?.Token ?? Guid.Empty;
            var documentBytes = request?.DocumentBytes;

            var session = _editSessionRepository.GetByToken(token);
            if (session == null)
            {
                return NotFound(new { message = "Sitzung nicht gefunden oder abgelaufen." });
            }

            if (string.IsNullOrEmpty(documentBytes))
            {
                return BadRequest(new { message = "Keine Dokumentdaten empfangen." });
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(documentBytes);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Ungültige Base64-Daten." });
            }

            _editSessionRepository.UpdateStagedContent(token, bytes);
            Console.WriteLine($"Dokument gestaged: Token={token}, DocumentObjektId={session.DocumentObjektId}, Größe={bytes.Length} Bytes");

            session = _editSessionRepository.GetByToken(token);
            if (session == null || !session.HasPendingChanges || session.StagedFileStream == null)
            {
                return BadRequest(new { message = "Staging fehlgeschlagen." });
            }

            HttpContext.Session.SetInt32("LoginMandantId", session.MandantId);

            var newVersion = new DokumentVersion
            {
                CurrentVersionId = Guid.Empty,
                DocumentObjektId = session.DocumentObjektId,
                VersionNr = null,
                IsActive = true,
                IsArchived = false,
                CreatedDate = DateTime.Now,
                CreatedBy = session.UserId,
                FileStream = bytes,
                ContentType = session.ContentType,
                ContentSize = session.StagedFileStream.Length,
                FullContentSize = session.StagedFileStream.Length,
                FileExtension = Path.GetExtension(session.FileName),
                IsSigned = false,
                Final = true
            };

            try
            {
                _eAkteRepo.UpdateDokument(
                    dokumentObjektId: session.DocumentObjektId,
                    name: Path.GetFileNameWithoutExtension(session.FileName),
                    contentType: session.ContentType,
                    newVersion: newVersion,
                    modifierName: session.UserId);

                _editSessionRepository.Delete(token);
                _eAkteRepo.UnlockDokument(session.DocumentObjektId); // Lock aufheben, sonst bleibt das Dokument dauerhaft gesperrt
                return Ok();
            }
            catch (RepositoryException ex)
            {
                Console.WriteLine($"Fehler beim Übernehmen der Word-Bearbeitung: {ex}");
                return StatusCode(500, new { message = "Fehler beim Speichern der Version." });
            }
        }

        [TokenAuthorize(TokenSessionSource.DokumentEdit)]
        [HttpPost("confirm-edit-sameVersion")]
        public IActionResult ConfirmEditSameVersion([FromBody] ConfirmEditRequestDto request)
        {
            Console.WriteLine($"ConfirmEditSameVersion aufgerufen: Token={request?.Token}, DocumentBytesLength={request?.DocumentBytes?.Length ?? 0}");

            var token = request?.Token ?? Guid.Empty;
            var documentBytes = request?.DocumentBytes;

            var session = _editSessionRepository.GetByToken(token);
            if (session == null)
            {
                return NotFound(new { message = "Sitzung nicht gefunden oder abgelaufen." });
            }

            if (string.IsNullOrEmpty(documentBytes))
            {
                return BadRequest(new { message = "Keine Dokumentdaten empfangen." });
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(documentBytes);
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Ungültige Base64-Daten." });
            }

            _editSessionRepository.UpdateStagedContent(token, bytes);
            Console.WriteLine($"Dokument gestaged (SameVersion): Token={token}, DocumentObjektId={session.DocumentObjektId}, Größe={bytes.Length} Bytes");

            session = _editSessionRepository.GetByToken(token);
            if (session == null || !session.HasPendingChanges || session.StagedFileStream == null)
            {
                return BadRequest(new { message = "Staging fehlgeschlagen." });
            }

            HttpContext.Session.SetInt32("LoginMandantId", session.MandantId);

            try
            {
                _eAkteRepo.UpdateDokumentSameVersion(
                    dokumentObjektId: session.DocumentObjektId,
                    fileStream: bytes,
                    contentType: session.ContentType,
                    modifierName: session.UserId);

                _editSessionRepository.Delete(token);
                _eAkteRepo.UnlockDokument(session.DocumentObjektId); // Lock aufheben
                return Ok();
            }
            catch (RepositoryException ex)
            {
                Console.WriteLine($"Fehler beim Übernehmen der Word-Bearbeitung (SameVersion): {ex}");
                return StatusCode(500, new { message = "Fehler beim Speichern der Version." });
            }
        }

        // Seite aktualisieren, wenn die Bearbeitung abgeschlossen ist (session = null).
        [HttpGet("edit-session-status")]
        public IActionResult GetStatus(Guid token)
        {
            var session = _editSessionRepository.GetByToken(token);
            // Wenn die Session null ist, bedeutet das, dass sie entweder abgelaufen oder bereits gelöscht wurde.
            return Json(new { completed = session == null });
        }




        // Aktionen-Taste Dropdown.  
        [HttpGet("vorgaenge-by-akte/{objektId}")] //Dropdown Aktionen: Vorgang bearbeiten.
        public async Task<IActionResult> GetVorgaengeByAkte(Guid objektId)
        {
            try
            {
                var akte = await SharedEAkte.GetAkteByObjektId(objektId);

                if (akte == null) return NotFound();

                var vorgaenge = await SharedEAkte.GetVorgaengeByZulassungsnummer(akte.Zulassungsnummer);
                return Json(vorgaenge.Select(v => new { v.Aktenzeichen, v.ObjektId }));
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Vorgänge für Akte {ObjektId}", objektId);
                return StatusCode(500);
            }
        }

        // Vorlage-Dropdown.
        [HttpGet("vorlagen-by-vorgangsTypName-and-ablageklasse")]
        public IActionResult GetVorlagen([FromQuery] Guid objectId)
        {
            try
            {
                var ordner = _eAkteRepo.GetOrdnerInfo(objectId);
                if (ordner == null) return NotFound();

                string? vorgangsTypName = null;

                if (!string.IsNullOrWhiteSpace(ordner.Aktenzeichen))
                {
                    vorgangsTypName = _eAkteRepo.GetVorgangsTypNameByAktenzeichen(ordner.Aktenzeichen);
                }

                var vorlagen = _eAkteRepo.GetVorlageVersionen(ordner.SubType, vorgangsTypName);

                if (vorlagen == null) return NotFound();

                return Json(vorlagen.Select(v => new
                {
                    v.VorlageVersionId,
                    v.VorlageName,
                    v.VorlageObjektTyp,
                    v.IstTagBefuellungAktiv
                }));
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Vorlagen.");
                return StatusCode(500);
            }
        }

        [HttpPost("dokument-aus-vorlage-erzeugen")]
        public async Task<IActionResult> ErzeugeDokumentAusVorlage([FromBody] DokumentAusVorlageRequest request)
        {
            try
            {
                var ersteller = HttpContext.Session.GetString("UserName") ?? "system";

                var ordner = await SharedEAkte.GetOrdnerByObjektId(request.FolderId);

                if (ordner == null) return NotFound();

                var vorgang = await SharedEAkte.GetVorgangByAktenzeichen(ordner.Aktenzeichen);

                if (vorgang == null) return NotFound();

                // Nummer generieren: nnnn.yy, jährlich zurückgesetzt.
                var currentYearSuffix = DateTime.Now.ToString("yy");
                var alleDokumente = await SharedEAkte.GetAllDokumente();
                var maxNumber = alleDokumente
                    .Select(d => d.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n) && n.EndsWith("." + currentYearSuffix))
                    .Select(n => n.Substring(0, n.Length - (currentYearSuffix.Length + 1)))
                    .Select(n => long.TryParse(n, out var num) ? num : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                var nextNumber = maxNumber + 1;
                var generatedName = $"{nextNumber}.{currentYearSuffix}";

                var objektTypId = _eAkteRepo.GetVorlageObjektTypId(request.VorlageVersionId);
                var platzhalter = VorlagePlatzhalterBuilder.Build(objektTypId, vorgang, ersteller);

                _eAkteRepo.ErzeugeDokumentAusVorlage(
                    name: generatedName,
                    vorlageVersionId: request.VorlageVersionId,
                    folderId: request.FolderId,
                    ersteller: ersteller,
                    aktenzeichen: ordner.Aktenzeichen,
                    platzhalter: platzhalter);



                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Erzeugen des Dokuments aus Vorlage.");
                return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        public static class VorlagePlatzhalterBuilder
        {
            private static readonly string[] Gemeinsam =
            {
                "AbsenderOrganisation", "AbsenderAdresse", "AbsenderPLZ", "AbsenderOrt",
                "EmpfaengerOrganisation", "EmpfaengerStrasse", "EmpfaengerPLZ", "EmpfaengerOrt",
                "EmpfaengerLand", "Stelle", "Abteilung", "Referat", "Ersteller", "Bearbeiter",
                "BearbeiterMail", "BearbeiterTel", "Aktenzeichen", "Zulassungsnummer",
                "Absender", "Ansprechpartner", "AnsprechpartnerTel", "AnsprechpartnerEmail", "Fax"
            };

            private static readonly Dictionary<int, string[]> TagsProTyp = new()
            {
                [2] = Gemeinsam.Concat(new[] { "Eingangsdatum", "Subject" }).ToArray(),

                [4] = Gemeinsam.Concat(new[]
                {
                    "Eingangsdatum", "Gegenstand", "GebStorniert", "GebInBearbeitung", "GebBezahlt",
                    "GebOffen", "GebUnterzahlt", "GebUeberzahlt", "AbsenderStrasse", "ErzeugtAm"
                }).ToArray(),

                [5] = Gemeinsam.Concat(new[] { "DatumAntrag", "Gegenstand", "Subject" }).ToArray(),
            };

            public static Dictionary<string, string> Build(int? objektTypId, VorgangDto vorgang, string ersteller)
            {
                var jetzt = DateTime.Now.ToString("dd.MM.yyyy");

                // Bekannte Werte (alles, was wir aus dem Vorgang / Kontext sicher haben)
                var bekannt = new Dictionary<string, string?>
                {
                    ["Ansprechpartner"] = vorgang.Ansprechpartner,
                    ["Abteilung"] = vorgang.Abteilung,
                    ["Referat"] = vorgang.Referat,
                    ["Stelle"] = vorgang.Stelle,
                    ["Aktenzeichen"] = vorgang.Aktenzeichen,
                    ["Zulassungsnummer"] = vorgang.Zulassungsnummer,
                    ["Subject"] = vorgang.Betreff,
                    ["Gegenstand"] = vorgang.Betreff,
                    ["Ersteller"] = ersteller,
                    ["Bearbeiter"] = ersteller,
                    ["ErzeugtAm"] = jetzt,
                    ["DatumAntrag"] = jetzt,
                    ["Eingangsdatum"] = jetzt,
                    // TODO: Absender*, Empfaenger*, BearbeiterMail/Tel, AnsprechpartnerTel/Email, Fax, Geb
                };

                // Unbekannter Typ -> altes Verhalten (nur bekannte Werte)
                if (objektTypId == null || !TagsProTyp.TryGetValue(objektTypId.Value, out var tags))
                {
                    return bekannt.ToDictionary(k => k.Key, k => k.Value ?? string.Empty);
                }

                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var tag in tags.Distinct())
                {   // Distinct: z.B. Absender/AbsenderPLZ stehen bei Typ 4 doppelt
                    result[tag] = bekannt.TryGetValue(tag, out var v) ? v ?? string.Empty : string.Empty;
                }

                return result;
            }
        }




        // Modal Create/Edit Akte/Vorgang.     
        [HttpGet("get-modal-data-eakte")]
        public async Task<IActionResult> GetModalData(string? ObjectId, string objectType, string mode)
        {
            try
            {
                // Überprüfen, ob der Benutzer angemeldet ist und Email ermitteln.
                var email = HttpContext.Session.GetString("UserEmail");

                if (string.IsNullOrEmpty(email)) return Unauthorized();

                // Benutzerinformationen abrufen.
                var user = _loginRepository.GetUserByEmail(email: email);
                ViewBag.UserId = user?.UId;

                // Dropdown-Daten für Antragsarten, Vorgangstypen und Aktionstypen abrufen.
                ViewBag.Antragsarten = _eAkteRepo.GetAntragsarten();
                ViewBag.Vorgangstypen = _eAkteRepo.GetVorgangstypen();

                // ObjektId validieren.
                if (!Guid.TryParse(ObjectId, out var objektId)) return BadRequest("Invalid ObjectId");

                // Modus.
                ViewBag.Mode = mode;
                ViewBag.ObjectType = objectType;

                // Akte erstellen - click auf die Nebengruppe.
                if (mode == "create" && objectType == "akte")
                {
                    var nebengruppe = _eAkteRepo.GetNebengruppeNummerByObjektId(objektId: objektId);
                    var hauptgruppe = await SharedEAkte.GetHauptgruppeByHauptgruppeNummer(hauptgruppeNummer: nebengruppe.GehoertZuHauptgruppeNummer ?? 0);

                    var abteilungReferatPairs = _eAkteRepo.GetAbteilungReferatByObjektId(hauptgruppe.ObjektId);

                    // Einzigartige Abteilungen für den ersten Select.
                    ViewBag.Abteilung = abteilungReferatPairs.Select(p => p.Abteilung).Where(a => a != null).Distinct().ToList();
                    // JSON für die Abteilung-Referat-Paare, um die Referate basierend auf der ausgewählten Abteilung zu filtern.
                    ViewBag.AbteilungReferatJson = JsonSerializer.Serialize(abteilungReferatPairs.Select(p => new { abteilung = p.Abteilung, referat = p.Referat }));

                    var ansprechpartnerListe = _eAkteRepo.GetAnsprechpartnerByAbteilungReferatPaare(abteilungReferatPairs);
                    ViewBag.AnsprechpartnerListe = ansprechpartnerListe;

                    var maxLfdNr = hauptgruppe.LaufenderAktenzeichenZaehler + 1;
                    ViewBag.NebengruppeNummer = nebengruppe.NebengruppeNummer;
                    ViewBag.HauptgruppeNummer = nebengruppe.GehoertZuHauptgruppeNummer;
                    ViewBag.NebengruppeBezeichnung = nebengruppe.Bezeichnung;
                    ViewBag.HauptgruppeBezeichnung = hauptgruppe.Bezeichnung;
                    ViewBag.KategorieNummer = nebengruppe.GehoertZuKategorieNummer;
                    ViewBag.MaxLfdNr = maxLfdNr;
                    ViewBag.Zulassung = BuildZulassung(user: user, nb: (nebengruppe.NebengruppeNummer, nebengruppe.GehoertZuHauptgruppeNummer, nebengruppe.GehoertZuKategorieNummer), maxLfdNr: maxLfdNr);
                    var aktenzeichen = BuildAktenzeichen(user: user, nb: (nebengruppe.NebengruppeNummer, nebengruppe.GehoertZuHauptgruppeNummer, nebengruppe.GehoertZuKategorieNummer), maxLfdNr: maxLfdNr);
                    ViewBag.Aktenzeichen = aktenzeichen;
                    ViewBag.KontakteModel = BuildKontakteModel(new KontaktSearchModel(), aktenzeichen, 1, 10, "Name", "asc");
                    ViewBag.AblaufdatenModel = BuildAblaufdatenModel(new AblaufdatenSearchModel(), aktenzeichen, 1, 10, "Datum", "asc");
                    ViewBag.GebuehrenModel = LadeGebuehren(aktenzeichen); // TESTWERT: später durch aktenzeichen ersetzen
                }

                // Akte(Vorgang) bearbeiten - click auf die Akte - Dropdown (Vorgang auswählen).
                if (mode == "edit" && (objectType == "akte" || objectType == "vorgang"))
                {
                    var vorgang = await SharedEAkte.GetVorgangByObjektId(objektId: objektId);
                    var akte = await SharedEAkte.GetAkteByZulassungsnummer(zulassungsnummer: vorgang.Zulassungsnummer);
                    var nebengruppe = await SharedEAkte.GetNebengruppeByNebengruppeNummer(nebengruppeNummer: akte.NebengruppeNummer);
                    var hauptgruppe = await SharedEAkte.GetHauptgruppeByHauptgruppeNummer(hauptgruppeNummer: nebengruppe.GehoertZuHauptgruppeNummer);

                    ViewBag.Name = vorgang.ErzeugtVon;

                    ViewBag.Abteilung = _eAkteRepo.GetAbteilungLangFromVorgang(objektId: vorgang.ObjektId);
                    ViewBag.Referat = _eAkteRepo.GetReferatFromVorgang(objektId: vorgang.ObjektId);
                    ViewBag.Stelle = vorgang.Stelle;

                    // Status-Tabelle (Vorgangsstatus + letzte offene Aktion).
                    ViewBag.StatusUebersicht = _eAkteRepo.GetVorgangStatusUebersicht(vorgang.Aktenzeichen);


                    // Radio-Gruppen: Antrag / Bescheid.
                    ViewBag.AntragVorhanden = vorgang.AntragVorhanden;
                    ViewBag.BescheidNeufassung = vorgang.BescheidNeufassung;

                    ViewBag.Ansprechpartner = vorgang.Ansprechpartner;

                    var maxLfdNr = hauptgruppe.LaufenderAktenzeichenZaehler + 1;
                    ViewBag.NebengruppeNummer = nebengruppe.NebengruppeNummer;
                    ViewBag.HauptgruppeNummer = nebengruppe.GehoertZuHauptgruppeNummer;
                    ViewBag.NebengruppeBezeichnung = nebengruppe.Bezeichnung;
                    ViewBag.HauptgruppeBezeichnung = hauptgruppe.Bezeichnung;
                    ViewBag.KategorieNummer = nebengruppe.GehoertZuKategorieNummer;
                    ViewBag.MaxLfdNr = maxLfdNr;
                    ViewBag.Zulassung = vorgang.Zulassungsnummer;
                    ViewBag.Aktenzeichen = vorgang.Aktenzeichen;
                    ViewBag.VorgangDaten = vorgang;
                    ViewBag.Antragsteller = _eAkteRepo.GetAntragstellerByAktenzeichen(vorgang.Aktenzeichen);
                    ViewBag.KontakteModel = BuildKontakteModel(new KontaktSearchModel(), vorgang.Aktenzeichen, 1, 10, "Name", "asc");
                    ViewBag.AblaufdatenModel = BuildAblaufdatenModel(new AblaufdatenSearchModel(), vorgang.Aktenzeichen, 1, 10, "Datum", "asc");
                    ViewBag.GebuehrenModel = LadeGebuehren(vorgang.Aktenzeichen); // TESTWERT: später durch aktenzeichen ersetzen
                }

                // Vorgang erstellen - click auf die Akte.
                if (mode == "create" && objectType == "vorgang")
                {
                    var akte = await SharedEAkte.GetAkteByObjektId(objektId: objektId);
                    var nebengruppe = await SharedEAkte.GetNebengruppeByNebengruppeNummer(nebengruppeNummer: akte.NebengruppeNummer);
                    var hauptgruppe = await SharedEAkte.GetHauptgruppeByHauptgruppeNummer(hauptgruppeNummer: nebengruppe.GehoertZuHauptgruppeNummer);

                    var abteilungReferatPairs = _eAkteRepo.GetAbteilungReferatByObjektId(hauptgruppe.ObjektId);

                    // Einzigartige Abteilungen für den ersten Select.
                    ViewBag.Abteilung = abteilungReferatPairs.Select(p => p.Abteilung).Where(a => a != null).Distinct().ToList();

                    // JSON für die Abteilung-Referat-Paare, um die Referate basierend auf der ausgewählten Abteilung zu filtern.
                    ViewBag.AbteilungReferatJson = JsonSerializer.Serialize(abteilungReferatPairs.Select(p => new { abteilung = p.Abteilung, referat = p.Referat }));


                    var ansprechpartnerListe = _eAkteRepo.GetAnsprechpartnerByAbteilungReferatPaare(abteilungReferatPairs);
                    ViewBag.AnsprechpartnerListe = ansprechpartnerListe;

                    var maxLfdNr = hauptgruppe.LaufenderAktenzeichenZaehler + 1;
                    ViewBag.NebengruppeNummer = nebengruppe.NebengruppeNummer;
                    ViewBag.HauptgruppeNummer = nebengruppe.GehoertZuHauptgruppeNummer;
                    ViewBag.NebengruppeBezeichnung = nebengruppe.Bezeichnung;
                    ViewBag.HauptgruppeBezeichnung = hauptgruppe.Bezeichnung;
                    ViewBag.KategorieNummer = nebengruppe.GehoertZuKategorieNummer;
                    ViewBag.MaxLfdNr = maxLfdNr;
                    ViewBag.Zulassung = akte.Zulassungsnummer;
                    var aktenzeichen = BuildAktenzeichen(user: user, nb: (nebengruppe.NebengruppeNummer, nebengruppe.GehoertZuHauptgruppeNummer, nebengruppe.GehoertZuKategorieNummer), maxLfdNr: maxLfdNr);
                    ViewBag.Aktenzeichen = aktenzeichen;
                    ViewBag.KontakteModel = BuildKontakteModel(new KontaktSearchModel(), aktenzeichen, 1, 10, "Name", "asc");
                    ViewBag.AblaufdatenModel = BuildAblaufdatenModel(new AblaufdatenSearchModel(), aktenzeichen, 1, 10, "Datum", "asc");
                    ViewBag.GebuehrenModel = LadeGebuehren(aktenzeichen); // TESTWERT: später durch aktenzeichen ersetzen
                }

                return PartialView("_CreateAkte", user);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden des Akte-Modals. ObjectType: {ObjectType}, Mode: {Mode}, ObjectId: {ObjectId}", objectType, mode, ObjectId);
                return StatusCode(500);
            }
        }
        // Hilfsmethoden zum Erstellen von Zulassungsnummer und Aktenzeichen.
        private string BuildZulassung(Personen? user, (int? NebengruppeNummer, int? GehoertZuHauptgruppeNummer, int? GehoertZuKategorieNumme) nb, int maxLfdNr)
        {
            var year = DateTime.Now.Year % 100;
            return $"{user?.Abteilung} {user?.Stelle}-{nb.GehoertZuKategorieNumme}.{nb.GehoertZuHauptgruppeNummer}.{nb.NebengruppeNummer}-{maxLfdNr}/{year}";
        }

        private string BuildAktenzeichen(Personen? user, (int? NebengruppeNummer, int? GehoertZuHauptgruppeNummer, int? GehoertZuKategorieNummer) nb, int maxLfdNr)
        {
            var year = DateTime.Now.Year % 100;
            return $"{user?.Abteilung} {user?.Stelle}-{nb.GehoertZuKategorieNummer}.{nb.GehoertZuHauptgruppeNummer}.{nb.NebengruppeNummer}-{maxLfdNr}/{year}";
        }




        // Antragsteller.
        [HttpGet("get-address-by-id/{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var adresse = _eAkteRepo.GetAdresseById(id: id);

                if (adresse == null) return NotFound();

                return Json(adresse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Adresse mit Id {Id}", id);
                return StatusCode(500);
            }
        }

        [HttpPost("save-antragsteller")]
        public IActionResult Save([FromQuery] string aktenzeichen, [FromBody] List<Antragsteller> items)
        {
            try
            {
                if (items == null || !items.Any()) return BadRequest("Keine Daten.");

                _eAkteRepo.SaveAntragsteller(aktenzeichen: aktenzeichen, items: items);
                return Json(new { success = true, message = "Gespeichert." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Antragsteller für Aktenzeichen {Aktenzeichen}", aktenzeichen);
                return Json(new { success = false, message = "Fehler beim Speichern der Antragsteller." });
            }
        }




        //Kontakte.
        [HttpGet("load-kontakte")]
        public IActionResult LoadKontakte(KontaktSearchModel search, string aktenzeichen, int page = 1, int pageSize = 10, string sortColumn = "Name", string sortDirection = "asc")
        {
            try
            {
                var model = BuildKontakteModel(search: search, aktenzeichen: aktenzeichen, page: page, pageSize: pageSize, sortColumn: sortColumn, sortDirection: sortDirection);
                return PartialView("_KontakteTable", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Kontakte für Aktenzeichen {Aktenzeichen}", aktenzeichen);
                return StatusCode(500);
            }
        }

        private KontaktViewModel BuildKontakteModel(KontaktSearchModel search, string aktenzeichen, int page, int pageSize, string sortColumn, string sortDirection)
        {
            IEnumerable<Kontakt> kontakte = _eAkteRepo.GetKontakteByAktenzeichen(aktenzeichen: aktenzeichen);

            if (!string.IsNullOrWhiteSpace(search.Anrede))
            {
                kontakte = kontakte.Where(k => k.Anrede != null &&
                    k.Anrede.Contains(search.Anrede, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.Vorname))
            {
                kontakte = kontakte.Where(k => k.Vorname != null &&
                                    k.Vorname.Contains(search.Vorname, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.Name))
            {
                kontakte = kontakte.Where(k => k.Name != null &&
                    k.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.Email))
            {
                kontakte = kontakte.Where(k => k.Email != null &&
                    k.Email.Contains(search.Email, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.EmailAnzeigename))
            {
                kontakte = kontakte.Where(k => k.EmailAnzeigename != null &&
                    k.EmailAnzeigename.Contains(search.EmailAnzeigename, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.EmailAn))
            {
                kontakte = kontakte.Where(k => k.EmailAn != null &&
                    k.EmailAn.Contains(search.EmailAn, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.Telefon))
            {
                kontakte = kontakte.Where(k => k.Telefon != null &&
                    k.Telefon.Contains(search.Telefon, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search.Fax))
            {
                kontakte = kontakte.Where(k => k.Fax != null &&
                    k.Fax.Contains(search.Fax, StringComparison.OrdinalIgnoreCase));
            }

            var sortMap = new Dictionary<string, Func<Kontakt, object?>>
            {
                { "Anrede",           k => k.Anrede },
                { "Vorname",          k => k.Vorname },
                { "Name",             k => k.Name },
                { "Email",            k => k.Email },
                { "EmailAnzeigename", k => k.EmailAnzeigename },
                { "EmailAn",          k => k.EmailAn },
                { "Telefon",          k => k.Telefon },
                { "Fax",              k => k.Fax }
            };

            if (!sortMap.ContainsKey(sortColumn))
            {
                sortColumn = "Name";
            }

            var list = sortDirection == "asc"
                ? kontakte.OrderBy(sortMap[sortColumn]).ToList()
                : kontakte.OrderByDescending(sortMap[sortColumn]).ToList();

            var totalCount = list.Count;
            var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new KontaktViewModel
            {
                Kontakte = paged,
                Search = search,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SortColumn = sortColumn,
                SortDirection = sortDirection
            };
        }

        [HttpPost("save-kontakt")]
        public IActionResult SaveKontakt([FromBody] Kontakt dto)
        {
            if (dto == null)
            {
                return Json(new { success = false, message = "Ungültige Anfrage." });
            }
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return Json(new { success = false, message = "Bitte füllen Sie das Feld \"Name\" aus." });
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return Json(new { success = false, message = "Bitte füllen Sie das Feld \"E-Mail Adresse\" aus." });
            }
            if (string.IsNullOrWhiteSpace(dto.Telefon))
            {
                return Json(new { success = false, message = "Bitte füllen Sie das Feld \"Telefon\" aus." });
            }
            var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            var telefonRegex = new Regex(@"^[0-9+\-\s()]{5,}$");
            if (!emailRegex.IsMatch(dto.Email.Trim()))
            {
                return Json(new { success = false, message = "Bitte geben Sie eine gültige E-Mail-Adresse ein." });
            }
            if (!telefonRegex.IsMatch(dto.Telefon.Trim()))
            {
                return Json(new { success = false, message = "Bitte geben Sie eine gültige Telefonnummer ein." });
            }
            try
            {
                if (dto.Id == 0)
                {
                    _eAkteRepo.InsertKontakt(dto: dto);
                }
                else
                {
                    _eAkteRepo.UpdateKontakt(dto: dto);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern des Kontakts Id {Id}", dto.Id);
                return Json(new { success = false, message = "Fehler beim Speichern des Kontakts." });
            }
        }

        [HttpDelete("delete-kontakt/{id}")]
        public IActionResult DeleteKontakt(int id)
        {
            try
            {
                _eAkteRepo.DeleteKontakt(id: id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Kontakts Id {Id}", id);
                return Json(new { success = false, message = "Fehler beim Löschen des Kontakts." });
            }
        }

        [HttpPost("parse-mail")] // Email Drag & Drop.
        public IActionResult ParseMail(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Keine Datei erhalten.");
            }

            // Prüfen, ob die Datei eine .msg oder .eml Datei ist.
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (ext != ".msg" && ext != ".eml")
            {
                return BadRequest("Nicht unterstütztes Dateiformat: " + ext);
            }

            using var stream = new MemoryStream();
            file.CopyTo(stream);
            stream.Position = 0;

            try
            {
                string senderName;
                string senderEmail;

                if (ext == ".msg")
                {
                    using var msg = new Storage.Message(stream);
                    senderName = msg.Sender?.DisplayName ?? "";
                    senderEmail = msg.Sender?.Email ?? "";
                }
                else // .eml
                {
                    var eml = Message.Load(stream);
                    senderName = eml.Headers?.From?.DisplayName ?? "";
                    senderEmail = eml.Headers?.From?.Address ?? "";
                }

                var teile = senderName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var vorname = teile.Length > 1 ? string.Join(' ', teile[..^1]) : "";
                var nachname = teile.Length > 1 ? teile[^1] : (teile.Length > 0 ? teile[0] : "");

                var result = new KontaktDaten
                {
                    Vorname = vorname,
                    Name = nachname,
                    Email = senderEmail,
                    Telefon = "" // in Mail-Headern normalerweise nicht enthalten
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fehler beim Parsen: {ex.Message}");
            }
        }




        //Ablaufdaten.  
        [HttpGet("load-ablaufdaten")]
        public IActionResult LoadAblaufdaten(AblaufdatenSearchModel search, string aktenzeichen, int page = 1, int pageSize = 10, string sortColumn = "AktionAm", string sortDirection = "asc")
        {
            try
            {
                var model = BuildAblaufdatenModel(search: search, aktenzeichen: aktenzeichen, page: page, pageSize: pageSize, sortColumn: sortColumn, sortDirection: sortDirection);
                return PartialView("_AblaufdatenTable", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Ablaufdaten für Aktenzeichen {Aktenzeichen}", aktenzeichen);
                return StatusCode(500);
            }
        }

        private AblaufdatenViewModel BuildAblaufdatenModel(AblaufdatenSearchModel search, string aktenzeichen, int page, int pageSize, string sortColumn, string sortDirection)
        {
            IEnumerable<Ablaufdaten> ablaufdaten = _eAkteRepo.GetAblaufdatenByAktenzeichen(aktenzeichen: aktenzeichen);

            // --- Filterung ---
            if (!string.IsNullOrWhiteSpace(search.EintragVon))
            {
                ablaufdaten = ablaufdaten.Where(a => a.EintragVon != null &&
                    a.EintragVon.Contains(search.EintragVon, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(search.AktionTyp))
            {
                ablaufdaten = ablaufdaten.Where(a => a.AktionTyp != null &&
                    a.AktionTyp.Contains(search.AktionTyp, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(search.Bemerkung))
            {
                ablaufdaten = ablaufdaten.Where(a => a.Bemerkung != null &&
                    a.Bemerkung.Contains(search.Bemerkung, StringComparison.OrdinalIgnoreCase));
            }
            if (search.EintragAmVom.HasValue)
            {
                ablaufdaten = ablaufdaten.Where(a => a.EintragAmVom.HasValue &&
                    a.EintragAmVom.Value.Date == search.EintragAmVom.Value.Date);
            }
            if (search.AktionAm.HasValue)
            {
                ablaufdaten = ablaufdaten.Where(a => a.AktionAm.HasValue &&
                    a.AktionAm.Value.Date == search.AktionAm.Value.Date);
            }

            // --- Sortierung ---
            var sortMap = new Dictionary<string, Func<Ablaufdaten, object?>>
            {
                { "EintragAmVom", a => a.EintragAmVom },
                { "EintragVon",   a => a.EintragVon },
                { "AktionAm",     a => a.AktionAm },
                { "AktionTyp",    a => a.AktionTyp },
                { "Bemerkung",    a => a.Bemerkung }
            };

            if (!sortMap.ContainsKey(sortColumn))
            {
                sortColumn = "AktionAm";
            }

            var list = sortDirection == "asc"
                ? ablaufdaten.OrderBy(sortMap[sortColumn]).ToList()
                : ablaufdaten.OrderByDescending(sortMap[sortColumn]).ToList();

            // --- Paginierung ---
            var totalCount = list.Count;
            var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new AblaufdatenViewModel
            {
                Ablaufdaten = paged,
                Search = search,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                SortColumn = sortColumn,
                SortDirection = sortDirection,
                Aktionstypen = _eAkteRepo.GetAktionstypen()
            };
        }

        [HttpPost("save-ablaufdaten")]
        public IActionResult SaveAblaufdaten([FromBody] Ablaufdaten dto)
        {
            if (dto == null)
            {
                return Json(new { success = false, message = "Ungültige Anfrage." });
            }

            if (dto.EintragAmVom == default)
            {
                return Json(new { success = false, message = "Bitte füllen Sie das Feld \"Eingetragen am\" aus." });
            }

            if (string.IsNullOrWhiteSpace(dto.EintragVon))
            {
                return Json(new { success = false, message = "Bitte füllen Sie das Feld \"Eingetragen von\" aus." });
            }

            if (dto.EintragVon.Trim().Length > 255)
            {
                return Json(new { success = false, message = "Feld \"Eingetragen von\" darf maximal 255 Zeichen lang sein." });
            }

            if (!string.IsNullOrWhiteSpace(dto.AktionTyp) && dto.AktionTyp.Trim().Length > 100)
            {
                return Json(new { success = false, message = "Feld \"Aktionstyp\" darf maximal 100 Zeichen lang sein." });
            }

            // Datumslogik: "Aktion am" darf nicht vor "Eingetragen am" liegen.
            if (dto.AktionAm.HasValue && dto.AktionAm.Value < dto.EintragAmVom)
            {
                return Json(new { success = false, message = "\"Aktion am\" darf nicht vor \"Eingetragen am\" liegen." });
            }

            try
            {
                if (dto.Id == 0)
                {
                    _eAkteRepo.InsertAblaufdaten(dto: dto);
                }
                else
                {
                    _eAkteRepo.UpdateAblaufdaten(dto: dto);
                }
                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Ablaufdaten Id {Id}", dto.Id);
                return Json(new { success = false, message = "Fehler beim Speichern der Ablaufdaten." });
            }
        }

        [HttpDelete("delete-ablaufdaten/{id}")]
        public IActionResult DeleteAblaufdaten(int id)
        {
            try
            {
                _eAkteRepo.DeleteAblaufdaten(id: id);
                return Json(new { success = true });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen der Ablaufdaten Id {Id}", id);
                return Json(new { success = false, message = "Fehler beim Löschen der Ablaufdaten." });
            }
        }




        // Gebühren.
        [HttpGet("load-gebuehre")]
        public IActionResult LoadGebuehre(string aktenzeichen)
        {
            try
            {
                return PartialView("_GebuehreTable", LadeGebuehren(aktenzeichen));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Gebühren für Aktenzeichen {Aktenzeichen}", aktenzeichen);
                return StatusCode(500);
            }
        }

        private List<GebuehrMitTitelViewModel> LadeGebuehren(string aktenzeichen)
        {
            return _eAkteRepo.GetGebuehrenByAktenzeichen(aktenzeichen)
                .OrderBy(g => IstVorschussTyp(g.Typ) ? 0 : 1)   // erst Vorschüsse, dann Gebühren
                .ThenBy(g => g.LfdNr)
                .Select(gebuehr => new GebuehrMitTitelViewModel
                {
                    Gebuehr = gebuehr,
                    Titel = _eAkteRepo.GetGebuehrTitelByAktenzeichenAndLfdNr(aktenzeichen, gebuehr.LfdNr)
                })
                .ToList();

        }

        private static bool IstVorschussTyp(string? typ) => TypBuchstabe(typ) == "V";

        // Erlaubte Funktionen für eine Gebühr (Typ + Status).
        [HttpGet("load-gebuehr-funktionen")]
        public IActionResult LoadGebuehrFunktionen(string typ, string? status)
        {
            try
            {
                var funktionen = _eAkteRepo.GetErlaubteFunktionen(TypBuchstabe(typ), status?.Trim().ToUpperInvariant() ?? string.Empty);
                return Json(funktionen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Funktionen für Typ {Typ}, Status {Status}", typ, status);
                return StatusCode(500);
            }
        }

        // Prüft, ob die Funktion für diese Gebühr (Typ + Status) erlaubt ist.
        private bool IstFunktionErlaubt(Gebuehr g, string funktion)
        {
            var erlaubt = _eAkteRepo.GetErlaubteFunktionen(TypBuchstabe(g.Typ), g.Status?.Trim().ToUpperInvariant() ?? string.Empty);
            return erlaubt.Any(f => f.Trim().Equals(funktion, StringComparison.OrdinalIgnoreCase));
        }

        private static string TypBuchstabe(string? typ)
        {
            var t = typ?.Trim() ?? string.Empty;
            if (t.Contains("Vorschuss", StringComparison.OrdinalIgnoreCase) || t.Equals("V", StringComparison.OrdinalIgnoreCase))
            {
                return "V";
            }
            if (t.Contains("Geb", StringComparison.OrdinalIgnoreCase) || t.Equals("G", StringComparison.OrdinalIgnoreCase))
            {
                return "G";
            }
            return t;
        }

        [HttpPost("gebuehr-stornieren")]
        public IActionResult GebuehrStornieren([FromForm] string aktenzeichen, [FromForm] int lfdNr)
        {
            try
            {
                var gebuehr = _eAkteRepo.GetGebuehrenByAktenzeichen(aktenzeichen).FirstOrDefault(g => g.LfdNr == lfdNr);

                if (gebuehr == null) return NotFound("Gebühr nicht gefunden.");
                if (!IstFunktionErlaubt(gebuehr, "Stornieren")) return BadRequest("Stornieren ist für diesen Status nicht erlaubt.");

                _eAkteRepo.StorniereGebuehr(aktenzeichen, lfdNr, Uid);

                Historie(aktenzeichen, "Gebühr storniert",$"{gebuehr.Typ}, LfdNr {lfdNr}, Kassenzeichen {gebuehr.Kassenzeichen}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Stornieren. Aktenzeichen {Aktenzeichen}, LfdNr {LfdNr}", aktenzeichen, lfdNr);
                return StatusCode(500, "Fehler beim Stornieren.");
            }
        }

        [HttpPost("gebuehr-loeschen")]
        public IActionResult GebuehrLoeschen([FromForm] string aktenzeichen, [FromForm] int lfdNr)
        {
            try
            {
                var gebuehr = _eAkteRepo.GetGebuehrenByAktenzeichen(aktenzeichen).FirstOrDefault(g => g.LfdNr == lfdNr);

                if (gebuehr == null) return NotFound("Gebühr nicht gefunden.");
                if (!IstFunktionErlaubt(gebuehr, "Löschen")) return BadRequest("Löschen ist für diesen Status nicht erlaubt.");

                _eAkteRepo.LoescheGebuehr(aktenzeichen, lfdNr);

                Historie(aktenzeichen, "Gebühr gelöscht", $"{gebuehr.Typ}, LfdNr {lfdNr}, Kassenzeichen {gebuehr.Kassenzeichen}");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen. Aktenzeichen {Aktenzeichen}, LfdNr {LfdNr}", aktenzeichen, lfdNr);
                return StatusCode(500, "Fehler beim Löschen.");
            }
        }

        [HttpPost("gebuehr-anlegen")]
        public IActionResult GebuehrAnlegen([FromBody] GebuehrNeuRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Aktenzeichen) || (req.Art != "V" && req.Art != "G")) return BadRequest("Ungültige Anfrage.");
                if (req.Titel.Count == 0 || req.Titel.Any(t => t.Betrag <= 0 || string.IsNullOrWhiteSpace(t.TitelNr))) return BadRequest("Mindestens ein Titel mit Betrag > 0 ist erforderlich.");

                _eAkteRepo.CreateGebuehr(req, Uid);

                Historie(req.Aktenzeichen, req.Art == "V" ? "Vorschussforderung angelegt" : "Gebührenforderung angelegt", $"Summe {req.Titel.Sum(t => t.Betrag):N2} €");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Anlegen. Aktenzeichen {Aktenzeichen}, Art {Art}", req?.Aktenzeichen, req?.Art);
                return StatusCode(500, "Fehler beim Anlegen.");
            }
        }

        [HttpPost("gebuehr-bearbeiten")]
        public IActionResult GebuehrBearbeiten([FromBody] GebuehrEditRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Aktenzeichen)) return BadRequest("Ungültige Anfrage.");
                if (req.Titel.Count == 0 || req.Titel.Any(t => t.Betrag <= 0 || string.IsNullOrWhiteSpace(t.TitelNr))) return BadRequest("Mindestens ein Titel mit Betrag > 0 ist erforderlich.");

                var gebuehr = _eAkteRepo.GetGebuehrenByAktenzeichen(req.Aktenzeichen).FirstOrDefault(g => g.LfdNr == req.LfdNr);
                if (gebuehr == null) return NotFound("Gebühr nicht gefunden.");
                if (!IstFunktionErlaubt(gebuehr, "Bearbeiten")) return BadRequest("Bearbeiten ist für diesen Status nicht erlaubt.");

                _eAkteRepo.UpdateGebuehr(req, User.Identity?.Name);

                Historie(req.Aktenzeichen, "Gebühr bearbeitet", $"{gebuehr.Typ}, LfdNr {req.LfdNr}, Summe {req.Titel.Sum(t => t.Betrag):N2} €");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Bearbeiten. Aktenzeichen {Aktenzeichen}, LfdNr {LfdNr}", req?.Aktenzeichen, req?.LfdNr);
                return StatusCode(500, "Fehler beim Bearbeiten.");
            }
        }

        // Historie schreiben; ein Fehler hier darf die bereits ausgeführte Aktion nicht scheitern lassen.
        private void Historie(string aktenzeichen, string aktionTyp, string? bemerkung)
        {
            try
            {
                _eAkteRepo.SchreibeAblaufdatum(aktenzeichen, aktionTyp, bemerkung, Uid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Historie konnte nicht geschrieben werden. Aktenzeichen {Aktenzeichen}, Aktion {Aktion}", aktenzeichen, aktionTyp);
            }
        }

        [HttpPost("gebuehr-status")]
        public IActionResult GebuehrStatus([FromBody] GebuehrStatusRequest req)
        {
            try
            {
                var neu = req.Status?.Trim().ToUpperInvariant() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(req.Aktenzeichen) || neu.Length != 1 || !"AZVKTOBXS".Contains(neu)) return BadRequest("Ungültiger Status.");

                var gebuehr = _eAkteRepo.GetGebuehrenByAktenzeichen(req.Aktenzeichen).FirstOrDefault(g => g.LfdNr == req.LfdNr);
                
                if (gebuehr == null) return NotFound("Gebühr nicht gefunden.");

                var alt = gebuehr.Status?.Trim().ToUpperInvariant();

                _eAkteRepo.SetzeGebuehrStatus(req.Aktenzeichen, req.LfdNr, neu, Uid);

                Historie(req.Aktenzeichen, "Status angepasst", $"{gebuehr.Typ}, LfdNr {req.LfdNr}, Status {alt} -> {neu}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Anpassen des Status. Aktenzeichen {Aktenzeichen}, LfdNr {LfdNr}", req?.Aktenzeichen, req?.LfdNr);
                return StatusCode(500, "Fehler beim Anpassen des Status.");
            }
        }



        // Akte / Vorgang - Erstellen / Bearbeiten. 
        [HttpPost("create-akte-vorgang")] //Erstellen der Akte, des Vorgangs und der Ordner in einem Schritt.  
        public IActionResult CreateAkte([FromBody] CreateAkteRequest req)
        {
            _eAkteRepo.IncrementLaufenderAktenzeichenZaehler(req.Akte.HauptgruppeNummer ?? 0); // Zähler erhöhen, damit die nächste Akte die richtige LfdNr bekommt.

            // Validierung.
            if (req?.Akte == null) return BadRequest(new { success = false, detail = "Akte fehlt" });
            if (string.IsNullOrEmpty(req.Akte.Zulassungsnummer)) return BadRequest(new { success = false, detail = "Zulassungsnummer fehlt" });
            if (req.Akte.LfdNr == null) return BadRequest(new { success = false, detail = "LfdNr fehlt" });
            if (req?.Vorgang == null) return BadRequest(new { success = false, detail = "Vorgang fehlt" });
            if (string.IsNullOrEmpty(req.Vorgang.Aktenzeichen)) return BadRequest(new { success = false, detail = "Aktenzeichen fehlt" });
            if (string.IsNullOrEmpty(req.Vorgang.Zulassungsnummer)) return BadRequest(new { success = false, detail = "Vorgang.Zulassungsnummer fehlt" });
            if (req?.Ordner == null) return BadRequest(new { success = false, detail = "Ordner fehlt" });
            if (req.Ordner.ParentObjectId == null) return BadRequest(new { success = false, detail = "Ordner.ParentObjectId fehlt" });

            try
            {
                // 1) Akte.
                _eAkteRepo.CreateAkte(
                    zulassungsnummer: req.Akte.Zulassungsnummer,
                    lfdNr: req.Akte.LfdNr.Value,
                    hauptgruppeNummer: req.Akte.HauptgruppeNummer,
                    nebengruppeNummer: req.Akte.NebengruppeNummer,
                    zustand: req.Akte.Zustand,
                    erzeugtVon: req.Akte.ErzeugtVon,
                    geandertVon: req.Akte.GeandertVon,
                    icon: null,
                    abteilung: req.Akte.Abteilung,
                    referat: req.Akte.Referat
                );

                // 2) Vorgang.
                var vorgangObjektId = _eAkteRepo.CreateVorgang(
                    aktenzeichen: req.Vorgang.Aktenzeichen,
                    userId: req.Vorgang.UserID,
                    stelle: req.Vorgang.Stelle,
                    zulassungsnummer: req.Vorgang.Zulassungsnummer,
                    datumAntrag: req.Vorgang.DatumAntrag ?? DateTime.Now,
                    antragsart: req.Vorgang.Antragsart,
                    bescheidtyp: req.Vorgang.Bescheidtyp,
                    gegenstand: req.Vorgang.Gegenstand,
                    status: req.Vorgang.Status,
                    betreff: req.Vorgang.Betreff,
                    bemerkung: req.Vorgang.Bemerkung,
                    objektId: req.Vorgang.ObjektId,
                    aktenzustand: req.Vorgang.Aktenzustand,
                    erzeugtVon: req.Vorgang.ErzeugtVon,
                    veroeffentlicht: req.Vorgang.Veroeffentlicht,
                    icon: null,
                    abteilung: req.Vorgang.Abteilung,
                    referat: req.Vorgang.Referat,
                    antragVorhanden: req.Vorgang.AntragVorhanden,
                    bescheidNeufassung: req.Vorgang.BescheidNeufassung,
                    ansprechpartner: req.Vorgang.Ansprechpartner
                );

                // 3) Ordner.
                var ordnerNamen = new[] { "Anlagen", "Bescheide", "Schriftverkehr", "Interner Schriftverkehr", "Prüfergebnisse und Nachweise" };
                foreach (var name in ordnerNamen)
                {
                    _eAkteRepo.CreateOrdner(
                        parentObjectId: vorgangObjektId,
                        name: name,
                        type: "Ordner",
                        creatorName: req.Ordner.CreatorName,
                        modifier: req.Ordner.Modifier,
                        ownerName: req.Ordner.OwnerName,
                        ownerPermit: req.Ordner.OwnerPermit ?? 0,
                        groupPermit: req.Ordner.GroupPermit ?? 0,
                        worldPermit: req.Ordner.WorldPermit ?? 0,
                        hauptsachgebiet: req.Ordner.Hauptsachgebiet,
                        sachgebiet: req.Ordner.Sachgebiet,
                        aktenzeichen: req.Ordner.Aktenzeichen,
                        lfdNr: req.Ordner.LfdNr ?? 0,
                        bemerkungen: req.Ordner.Bemerkungen,
                        status: req.Ordner.Status,
                        final: req.Ordner.Final,
                        path: req.Ordner.Path,
                        colorHex: req.Ordner.ColorHex,
                        aclName: req.Ordner.ACLName,
                        aclDomain: req.Ordner.ACLDomain,
                        isHidden: req.Ordner.IsHidden,
                        isDeleted: req.Ordner.IsDeleted,
                        isPublic: req.Ordner.IsPublic,
                        isSigned: req.Ordner.IsSigned
                    );
                }

                _eAkteRepo.InsertAblaufdaten(new Ablaufdaten
                {
                    Aktenzeichen = req.Vorgang.Aktenzeichen,
                    EintragVon = req.Vorgang.ErzeugtVon,
                    EintragAmVom = DateTime.Now,
                    AktionTyp = "Vorgang angelegt",
                    AktionAm = DateTime.Now,
                    Bemerkung = $"Vorgang {req.Vorgang.Aktenzeichen} mit Zulassungsnummer {req.Vorgang.Zulassungsnummer} angelegt von {req.Ordner.CreatorName}."
                });

                return Json(new { success = true });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL-Fehler beim Erstellen der Akte. Zulassungsnummer: {Zulassungsnummer}", req.Akte?.Zulassungsnummer);
                return StatusCode(500, new { success = false, error = "Datenbankfehler", detail = ex.Message });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Pflichtfeld fehlt beim Erstellen der Akte: {Field}", ex.ParamName);
                return BadRequest(new { success = false, error = "Pflichtfeld fehlt", detail = ex.Message });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Unerwarteter Fehler beim Erstellen der Akte. Zulassungsnummer: {Zulassungsnummer}", req.Akte?.Zulassungsnummer);
                return StatusCode(500, new { success = false, error = "Unerwarteter Fehler", detail = ex.Message });
            }
        }

        [HttpPost("create-vorgang")] // Alternative Methode: Nur Vorgang und Ordner erstellen, ohne Akte.
        public async Task<IActionResult> CreateVorgangOnly([FromBody] CreateAkteRequest req)
        {
            try
            {
                var akte = await SharedEAkte.GetAkteByZulassungsnummer(req.Vorgang.Zulassungsnummer);
                _eAkteRepo.IncrementLaufenderAktenzeichenZaehler(akte.HauptgruppeNummer);

                var vorgangObjektId = _eAkteRepo.CreateVorgang(
                    aktenzeichen: req.Vorgang.Aktenzeichen,
                    userId: req.Vorgang.UserID,
                    stelle: req.Vorgang.Stelle,
                    zulassungsnummer: req.Vorgang.Zulassungsnummer,
                    datumAntrag: req.Vorgang.DatumAntrag ?? DateTime.Now,
                    antragsart: req.Vorgang.Antragsart,
                    bescheidtyp: req.Vorgang.Bescheidtyp,
                    gegenstand: req.Vorgang.Gegenstand,
                    status: req.Vorgang.Status,
                    betreff: req.Vorgang.Betreff,
                    bemerkung: req.Vorgang.Bemerkung,
                    objektId: req.Vorgang.ObjektId,
                    aktenzustand: req.Vorgang.Aktenzustand,
                    erzeugtVon: req.Vorgang.ErzeugtVon,
                    veroeffentlicht: req.Vorgang.Veroeffentlicht,
                    icon: null,
                    abteilung: req.Vorgang.Abteilung,
                    referat: req.Vorgang.Referat,
                    antragVorhanden: req.Vorgang.AntragVorhanden,
                    bescheidNeufassung: req.Vorgang.BescheidNeufassung,
                    ansprechpartner: req.Vorgang.Ansprechpartner
                );

                var ordnerNamen = new[] { "Anlagen", "Bescheide", "Schriftverkehr", "Interner Schriftverkehr", "Prüfergebnisse und Nachweise" };
                foreach (var name in ordnerNamen)
                {
                    _eAkteRepo.CreateOrdner(
                        parentObjectId: vorgangObjektId,
                        name: name,
                        type: "Ordner",
                        creatorName: req.Ordner.CreatorName,
                        modifier: req.Ordner.Modifier,
                        ownerName: req.Ordner.OwnerName,
                        ownerPermit: req.Ordner.OwnerPermit ?? 0,
                        groupPermit: req.Ordner.GroupPermit ?? 0,
                        worldPermit: req.Ordner.WorldPermit ?? 0,
                        hauptsachgebiet: req.Ordner.Hauptsachgebiet,
                        sachgebiet: req.Ordner.Sachgebiet,
                        aktenzeichen: req.Ordner.Aktenzeichen,
                        lfdNr: req.Ordner.LfdNr ?? 0,
                        bemerkungen: req.Ordner.Bemerkungen,
                        status: req.Ordner.Status,
                        final: req.Ordner.Final,
                        path: req.Ordner.Path,
                        colorHex: req.Ordner.ColorHex,
                        aclName: req.Ordner.ACLName,
                        aclDomain: req.Ordner.ACLDomain,
                        isHidden: req.Ordner.IsHidden,
                        isDeleted: req.Ordner.IsDeleted,
                        isPublic: req.Ordner.IsPublic,
                        isSigned: req.Ordner.IsSigned
                    );
                }

                _eAkteRepo.InsertAblaufdaten(new Ablaufdaten
                {
                    Aktenzeichen = req.Vorgang.Aktenzeichen,
                    EintragVon = req.Vorgang.ErzeugtVon,
                    EintragAmVom = DateTime.Now,
                    AktionTyp = "Vorgang angelegt",
                    AktionAm = DateTime.Now,
                    Bemerkung = $"Vorgang {req.Vorgang.Aktenzeichen} mit Zulassungsnummer {req.Vorgang.Zulassungsnummer} angelegt von {req.Ordner.CreatorName}."
                });

                return Json(new { success = true });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL-Fehler beim Erstellen des Vorgangs. Aktenzeichen: {Aktenzeichen}", req.Vorgang?.Aktenzeichen);
                return StatusCode(500, new { success = false, error = "Datenbankfehler", detail = ex.Message });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des Vorgangs. Aktenzeichen: {Aktenzeichen}", req.Vorgang?.Aktenzeichen);
                return StatusCode(500, new { success = false, error = "Unerwarteter Fehler", detail = ex.Message });
            }
        }

        [HttpPost("update-vorgang")] // Vorgang aktualisieren.
        public IActionResult UpdateVorgang([FromQuery] string aktenzeichen, [FromBody] Vorgang req)
        {
            if (req == null) return BadRequest(new { success = false, detail = "Request fehlt" });
            if (string.IsNullOrEmpty(req.Aktenzeichen)) return BadRequest(new { success = false, detail = "Aktenzeichen fehlt" });

            try
            {
                _eAkteRepo.UpdateVorgang(
                    aktenzeichen: aktenzeichen,
                    datumAntrag: req.DatumAntrag ?? DateTime.Now,
                    antragsart: req.Antragsart,
                    bescheidtyp: req.Bescheidtyp,
                    gegenstand: req.Gegenstand,
                    status: req.Status,
                    betreff: req.Betreff,
                    bemerkung: req.Bemerkung,
                    aktenzustand: req.Aktenzustand,
                    veroeffentlicht: req.Veroeffentlicht,
                    erzeugtVon: req.ErzeugtVon,
                    erzeugtAm: req.ErzeugtAm ?? DateTime.Now,
                    antragVorhanden: req.AntragVorhanden,
                    bescheidNeufassung: req.BescheidNeufassung,
                    ansprechpartner: req.Ansprechpartner
                );

                // Nur GeandertVon/GeandertAm der zugehörigen Akte aktualisieren.
                if (!string.IsNullOrWhiteSpace(req.GeandertVon))
                {
                    _eAkteRepo.UpdateAkteGeandertInfo(
                         zulassungsnummer: req.Zulassungsnummer,
                         geandertVon: req.GeandertVon,
                         geandertAm: req.GeandertAm ?? DateTime.Now
                     );
                }

                _eAkteRepo.InsertAblaufdaten(new Ablaufdaten
                {
                    Aktenzeichen = aktenzeichen,
                    EintragVon = req.ErzeugtVon,
                    EintragAmVom = DateTime.Now,
                    AktionTyp = "Vorgang aktualisiert",
                    AktionAm = DateTime.Now,
                    Bemerkung = $"Vorgang {aktenzeichen} aktualisiert von {req.ErzeugtVon}."
                });

                return Json(new { success = true });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL-Fehler beim Aktualisieren des Vorgangs. Aktenzeichen: {Aktenzeichen}", aktenzeichen);
                return StatusCode(500, new { success = false, error = "Datenbankfehler", detail = ex.Message });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Vorgangs. Aktenzeichen: {Aktenzeichen}", aktenzeichen);
                return StatusCode(500, new { success = false, error = "Unerwarteter Fehler", detail = ex.Message });
            }
        }




        // Load Modal - Kategorie / Hauptgruppe / Nebengruppe.
        [HttpGet("get-modal-data-ka-ha-ne")]
        public async Task<IActionResult> GetModalDataKaHaNe(string objectType, string mode, Guid ObjectId, Guid ParentObjectId)
        {
            try
            {
                if (!IsAdminModeActive())
                {
                    return Forbid();
                }

                var viewModel = new CreateKaHaNeViewModel
                {
                    ObjectType = objectType,
                    Mode = mode,
                    ObjectId = ObjectId,
                    ParentObjectId = ParentObjectId
                };

                if (mode == "edit")
                {
                    switch (objectType)
                    {
                        case "kategorie":
                            var kategorieDto = await SharedEAkte.GetKategorieByObjektId(ObjectId);
                            if (kategorieDto == null) return NotFound();
                            viewModel.Kategorie = new Kategorie
                            {
                                Id = kategorieDto.Id,
                                KategorieNummer = kategorieDto.KategorieNummer,
                                Bezeichnung = kategorieDto.Bezeichnung,
                                ObjektId = kategorieDto.ObjektId,
                                Beschreibung = kategorieDto.Beschreibung,
                                Kuerzel = kategorieDto.Kuerzel,
                                Icon = kategorieDto.Icon
                            };

                            // Alle Abteilungen abrufen und die ausgewählten Abteilungen für diese Kategorie setzen.
                            viewModel.Abteilungen = _eAkteRepo.GetAllAbteilungen();
                            viewModel.SelectedAbteilungen = _eAkteRepo.GetAbteilungenByObjektId(ObjectId);

                            break;

                        case "hauptgruppe":
                            var hauptgruppeDto = await SharedEAkte.GetHauptgruppeByObjektId(ObjectId);
                            if (hauptgruppeDto == null) return NotFound();
                            viewModel.Hauptgruppe = new Hauptgruppe
                            {
                                Id = hauptgruppeDto.Id,
                                GehoertZuKategorieNummer = hauptgruppeDto.GehoertZuKategorieNummer,
                                Bezeichnung = hauptgruppeDto.Bezeichnung,
                                Kuerzel = hauptgruppeDto.Kuerzel,
                                Beschreibung = hauptgruppeDto.Beschreibung,
                                HauptgruppeNummer = hauptgruppeDto.HauptgruppeNummer,
                                LaufenderAktenzeichenZaehler = hauptgruppeDto.LaufenderAktenzeichenZaehler,
                                ObjektId = hauptgruppeDto.ObjektId,
                                Icon = hauptgruppeDto.Icon
                            };

                            var kategorie = await SharedEAkte.GetKategorieByKategorieNummer(hauptgruppeDto.GehoertZuKategorieNummer);

                            // Abteilungen der Elternkategorie (nur Anzeige) + deren Referate (wählbar).
                            viewModel.Abteilungen = _eAkteRepo.GetAbteilungenByObjektId(kategorie.ObjektId);
                            viewModel.Referate = _eAkteRepo.GetReferateByAbteilungNamen(viewModel.Abteilungen);

                            // Bereits ausgewählte Referate dieser Hauptgruppe.
                            viewModel.SelectedAbteilungReferate = _eAkteRepo.GetAbteilungReferatByObjektId(ObjectId);

                            break;

                        case "nebengruppe":
                            var nebengruppeDto = await SharedEAkte.GetNebengruppeByObjektId(ObjectId);
                            if (nebengruppeDto == null) return NotFound();
                            viewModel.Nebengruppe = new Nebengruppe
                            {
                                Id = nebengruppeDto.Id,
                                GehoertZuKategorieNummer = nebengruppeDto.GehoertZuKategorieNummer,
                                GehoertZuHauptgruppeNummer = nebengruppeDto.GehoertZuHauptgruppeNummer,
                                Bezeichnung = nebengruppeDto.Bezeichnung,
                                Kuerzel = nebengruppeDto.Kuerzel,
                                Beschreibung = nebengruppeDto.Beschreibung,
                                NebengruppeNummer = nebengruppeDto.NebengruppeNummer,
                                ObjektId = nebengruppeDto.ObjektId,
                                Icon = nebengruppeDto.Icon
                            };
                            break;

                        default:
                            return BadRequest("Unbekannter ObjectType.");
                    }
                }
                else if (mode == "create")
                {
                    // Wenn wir eine neue Hauptgruppe oder Nebengruppe erstellen, müssen wir die Bezeichnung und das Icon der übergeordneten Kategorie oder Hauptgruppe abrufen.
                    switch (objectType)
                    {
                        case "hauptgruppe":
                            var parentKategorie = await SharedEAkte.GetKategorieByObjektId(ParentObjectId);
                            viewModel.ParentBezeichnung = parentKategorie?.Bezeichnung;
                            viewModel.ParentIcon = parentKategorie?.Icon;

                            // Abteilungen der Elternkategorie (nur Anzeige) + deren Referate (wählbar, noch nichts ausgewählt).
                            viewModel.Abteilungen = _eAkteRepo.GetAbteilungenByObjektId(ParentObjectId);
                            viewModel.Referate = _eAkteRepo.GetReferateByAbteilungNamen(viewModel.Abteilungen);
                            break;

                        case "nebengruppe":
                            var parentHauptgruppe = await SharedEAkte.GetHauptgruppeByObjektId(ParentObjectId);
                            viewModel.ParentBezeichnung = parentHauptgruppe?.Bezeichnung;
                            viewModel.ParentIcon = parentHauptgruppe?.Icon;
                            break;

                        case "kategorie":
                            var abteilungen = _eAkteRepo.GetAllAbteilungen();
                            viewModel.Abteilungen = abteilungen;
                            break;

                    }
                }

                return PartialView("_CreateKaHaNe", viewModel);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Kategorie/Hauptgruppe/Nebengruppe-Daten.");
                return StatusCode(500);
            }
        }

        [HttpPost("update-ka-ha-ne")]
        public async Task<IActionResult> UpdateKaHaNe([FromBody] SaveKaHaNeRequest request)
        {
            try
            {
                if (!IsAdminModeActive())
                {
                    return Forbid();
                }

                // "new" - Bytes aus Base64, "remove" - null (löschen), "keep" - Repository die Spalte NICHT anfasst.
                byte[]? icon = request.IconAction switch
                {
                    "new" => Convert.FromBase64String(request.IconBase64 ?? ""),
                    "remove" => null,
                    _ => null // "keep" behandeln wir separat unten
                };

                bool touchIcon = request.IconAction is "new" or "remove";

                switch (request.ObjectType)
                {
                    case "kategorie":
                        _eAkteRepo.UpdateKategorie(objektId: request.ObjectId, bezeichnung: request.Bezeichnung, kuerzel: request.Kuerzel, beschreibung: request.Beschreibung, icon: icon, touchIcon: touchIcon);
                        _eAkteRepo.SaveAbteilungenForKategorie(objektId: request.ObjectId, abteilungen: request.Abteilungen);
                        break;

                    case "hauptgruppe":
                        _eAkteRepo.UpdateHauptgruppe(objektId: request.ObjectId, bezeichnung: request.Bezeichnung, kuerzel: request.Kuerzel, beschreibung: request.Beschreibung, icon: icon, touchIcon: touchIcon);
                        _eAkteRepo.SaveReferateForHauptgruppe(objektId: request.ObjectId, referatIds: request.Referate);
                        break;

                    case "nebengruppe":
                        _eAkteRepo.UpdateNebengruppe(objektId: request.ObjectId, bezeichnung: request.Bezeichnung, kuerzel: request.Kuerzel, beschreibung: request.Beschreibung, icon: icon, touchIcon: touchIcon);
                        break;

                    default:
                        return BadRequest("Unbekannter ObjectType.");
                }

                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren.");
                return StatusCode(500);
            }
        }

        [HttpPost("create-ka-ha-ne")]
        public async Task<IActionResult> CreateKaHaNe([FromBody] SaveKaHaNeRequest request)
        {
            try
            {
                if (!IsAdminModeActive())
                {
                    return Forbid();
                }

                byte[]? icon = request.IconAction == "new"
                    ? Convert.FromBase64String(request.IconBase64 ?? "")
                    : null;

                switch (request.ObjectType)
                {
                    case "kategorie":
                        var newOKategorieObjektId = _eAkteRepo.CreateKategorie(bezeichnung: request.Bezeichnung, kuerzel: request.Kuerzel, beschreibung: request.Beschreibung, icon: icon);
                        _eAkteRepo.SaveAbteilungenForKategorie(objektId: newOKategorieObjektId, abteilungen: request.Abteilungen);
                        break;

                    case "hauptgruppe":
                        // ParentObjectId - müsste zuerst in KategorieNummer aufgelöst werden.
                        var kategorie = await SharedEAkte.GetKategorieByObjektId(request.ParentObjectId);
                        if (kategorie == null) return BadRequest("Übergeordnete Kategorie nicht gefunden.");
                        var newHauptgruppeObjektId = _eAkteRepo.CreateHauptgruppe(kategorie.KategorieNummer, bezeichnung: request.Bezeichnung, kuerzel: request.Kuerzel, beschreibung: request.Beschreibung, icon: icon);
                        _eAkteRepo.SaveReferateForHauptgruppe(objektId: newHauptgruppeObjektId, referatIds: request.Referate);
                        break;

                    case "nebengruppe":
                        var hauptgruppe = await SharedEAkte.GetHauptgruppeByObjektId(request.ParentObjectId);
                        if (hauptgruppe == null) return BadRequest("Übergeordnete Hauptgruppe nicht gefunden.");
                        _eAkteRepo.CreateNebengruppe(gehoertZuKategorieNummer: hauptgruppe.GehoertZuKategorieNummer, gehoertZuHauptgruppeNummer: hauptgruppe.HauptgruppeNummer, bezeichnung: request.Bezeichnung, kuerzel: request.Kuerzel, beschreibung: request.Beschreibung, icon: icon);
                        break;

                    default:
                        return BadRequest("Unbekannter ObjectType.");
                }

                return Ok();
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen.");
                return StatusCode(500);
            }
        }

        [HttpPost("delete-ka-ha-ne")]
        public async Task<IActionResult> DeleteKaHaNe([FromBody] DeleteKaHaNeRequest request)
        {
            try
            {
                if (!IsAdminModeActive())
                {
                    return Forbid();
                }

                switch (request.ObjectType)
                {
                    case "Kategorie":
                        _eAkteRepo.DeleteKategorie(request.ObjectId);
                        break;
                    case "Hauptgruppe":
                        _eAkteRepo.DeleteHauptgruppe(request.ObjectId);
                        break;
                    case "Nebengruppe":
                        _eAkteRepo.DeleteNebengruppe(request.ObjectId);
                        break;
                    default:
                        return BadRequest("Unbekannter ObjectType.");
                }
                return Ok();
            }
            catch (RepositoryHasChildrenException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen.");
                return StatusCode(500);
            }
        }




        // Clipboard Copy / Move Actions.
        [HttpPost("clipboard/copy-here")]
        public IActionResult CopyHere([FromBody] ClipboardActionRequest request)
        {
            if (request?.SourceObjectIds == null || request.SourceObjectIds.Count == 0 || string.IsNullOrWhiteSpace(request.TargetObjectId))
            {
                return BadRequest(new { success = false, message = "Ungültige Anfrage." });
            }

            if (!Guid.TryParse(request.TargetObjectId, out var destinationId))
            {
                return BadRequest(new { success = false, message = "Ungültige Ziel-Id." });
            }

            if (request.SourceObjectIds.Count == 1 && string.Equals(request.SourceType, "UnterOrdner", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "Ordner können nicht kopiert werden, nur verschoben." });
            }

            List<Guid> sourceGuids;

            try
            {
                sourceGuids = request.SourceObjectIds.Select(Guid.Parse).ToList();
            }

            catch (FormatException)
            {
                return BadRequest(new { success = false, message = "Ungültige Quell-Id in der Liste." });
            }

            try
            {
                // Wenn nur ein Element in der Liste ist, rufen wir die Überladung für ein einzelnes Element auf, andernfalls die Überladung für eine Liste.
                int result = sourceGuids.Count == 1 ? _restClass.CopyObject(destinationId, sourceGuids[0]) : _restClass.CopyObject(destinationId, sourceGuids);

                return Ok(new { success = result == 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Kopieren nach {TargetObjectId}", request.TargetObjectId);
                return StatusCode(500, new { success = false, message = "Fehler beim Kopieren." });
            }
        }

        [HttpPost("clipboard/move-here")]
        public IActionResult MoveHere([FromBody] ClipboardActionRequest request)
        {
            if (request?.SourceObjectIds == null || request.SourceObjectIds.Count == 0 || string.IsNullOrWhiteSpace(request.TargetObjectId))
            {
                return BadRequest(new { success = false, message = "Ungültige Anfrage." });
            }

            if (!Guid.TryParse(request.TargetObjectId, out var destinationId))
            {
                return BadRequest(new { success = false, message = "Ungültige Ziel-Id." });
            }

            try
            {
                // Einzelnes Element, das ein Ordner ist - rekursive String-Überladung mit Typ.
                if (request.SourceObjectIds.Count == 1 && string.Equals(request.SourceType, "UnterOrdner", StringComparison.OrdinalIgnoreCase))
                {
                    int folderResult = _restClass.MoveObject(sDestinationFolderId: request.TargetObjectId, sSourceObjectId: request.SourceObjectIds[0], Typ: request.SourceType);

                    return Ok(new { success = folderResult == 1 });
                }

                List<Guid> sourceGuids;
                try
                {
                    sourceGuids = request.SourceObjectIds.Select(Guid.Parse).ToList();
                }
                catch (FormatException)
                {
                    return BadRequest(new { success = false, message = "Ungültige Quell-Id in der Liste." });
                }

                int docsResult = _restClass.MoveObject(destinationId, sourceGuids);
                return Ok(new { success = docsResult == 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Verschieben nach {TargetObjectId}", request.TargetObjectId);
                return StatusCode(500, new { success = false, message = "Fehler beim Verschieben." });
            }
        }




        // Kladde 
        [HttpPost("create-kladde-session")]
        public IActionResult CreateKladdeSession()
        {
            var token = Guid.NewGuid();
          
            _kladdeSessionRepository.Create(new KladdeSession
            {
                TokenId = token,
                Uid = Uid 
            });

            return Json(new { success = true, tokenId = token });
        }
    }
}