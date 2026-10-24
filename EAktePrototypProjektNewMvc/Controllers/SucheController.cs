using DreiF.Shared.Abstractions;
using DreiF.Shared.Exceptions;
using EAktePrototypProjektNewMvc.Exceptions;
using EAktePrototypProjektNewMvc.Helpers;
using EAktePrototypProjektNewMvc.Models.Akten;
using EAktePrototypProjektNewMvc.Models.Dokumente;
using EAktePrototypProjektNewMvc.Models.Ordner;
using EAktePrototypProjektNewMvc.Models.TypeSearch;
using EAktePrototypProjektNewMvc.Models.Vorgange;
using EAktePrototypProjektNewMvc.Repositories.Tree;
using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.Controllers
{
    [SessionAuthorize]
    [Route("bescheidbearbeitung")]
    public class SucheController : Controller
    {
        private readonly TypeSearchRepository _typeSearchRepo;
        private readonly ILogger<SucheController> _logger;
        private readonly DcmToWs.RestClass _restClass;

        public SucheController(IConfiguration config, ILogger<SucheController> logger, ICurrentMandantProvider mandantProvider)
        {
            _typeSearchRepo = new TypeSearchRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
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


        [HttpGet("search-results")]
        public IActionResult SearchResults()
        {
            // Holt den Suchzustand aus der Session.
            var state = HttpContext.Session.GetObject<SearchState>("Search_State");

            return PartialView("_SearchResult", state);
        }


        // Erweiterte Zugriffskontrolle, je nach Modus (Bescheidbearbeitung oder Mitzeichnung).
        private bool CheckAccessSearch(string uid, string nodeType, Guid objectId)
        {
            var mode = HttpContext.Session.GetString("AccessCheckMode") ?? "Mitzeichnung";

            if (mode == "Bescheidbearbeitung")
            {
                return _restClass.CheckAccessSecondVersion(uid, nodeType, objectId, IsAdminModeActive(), out _) > 0;
            }

            else // Mitzeichnung
            {
                return _restClass.CheckAccessMitzeichnung(uid, nodeType, objectId, out _) > 0;
            }
        }


        // Lädt die Suchergebnisse für Akten, Vorgänge oder Ordner basierend auf den übergebenen Suchparametern und sortiert sie entsprechend.
        [HttpGet("load-search-akte")]
        public IActionResult LoadSearchAkten(AkteSearchModel search, int globalPageSize = 250, int pageSize = 10, int page = 1, string? SortColumn = "ErzeugtAm", string? SortDirection = "asc")
        {
            try
            {
                var baseQuery = _typeSearchRepo.GetAktenSearchQuery(search, SortColumn, SortDirection);

                // Berechtigung.
                var filteredQuery = baseQuery
                    .Where(a => CheckAccessSearch(Uid, "Akte", a.ObjektId))
                    .ToList();

                // Gesamte Anzahl der Datensätze in der Datenbank.
                var totalCount = filteredQuery.Count();

                // Anzahl der Datensätze nach Anwendung des globalPageSize-Limits.
                var limitedCount = Math.Min(totalCount, globalPageSize);

                // Seite innerhalb der gültigen Grenzen halten.
                page = ClampPage(page, limitedCount, pageSize);

                // Speichert den aktuellen Suchzustand für den Typ "Akte" in der Session.
                SaveState("Akte", search, page, pageSize, globalPageSize, SortColumn, SortDirection);

                var paged = filteredQuery
                    .Take(globalPageSize)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AkteViewModel
                {
                    Akte = paged,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    LimitedCount = limitedCount,
                    SortColumn = SortColumn,
                    SortDirection = SortDirection
                };

                return PartialView("_AkteSucheTable", model);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Akten");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-search-vorgang")]
        public IActionResult LoadSearchVorgang(VorgangSearchModel search, int globalPageSize = 250, int pageSize = 10, int page = 1, string? SortColumn = "ErzeugtAm", string? SortDirection = "asc")
        {
            try
            {
                var baseQuery = _typeSearchRepo.GetVorgaengeSearchQuery(search, SortColumn, SortDirection);

                // Berechtigung.
                var filteredQuery = baseQuery
                    .Where(a => CheckAccessSearch(Uid, "Vorgang", a.ObjektId))
                    .ToList();

                var totalCount = filteredQuery.Count();
                var limitedCount = Math.Min(totalCount, globalPageSize);
                page = ClampPage(page, limitedCount, pageSize);
                SaveState("Vorgang", search, page, pageSize, globalPageSize, SortColumn, SortDirection);
                var paged = filteredQuery
                    .Take(globalPageSize)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                var model = new VorgangViewModel
                {
                    Vorgang = paged,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    LimitedCount = limitedCount,
                    SortColumn = SortColumn,
                    SortDirection = SortDirection
                };
                return PartialView("_VorgangSucheTable", model);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Vorgaenge");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }

        [HttpGet("load-search-ordner")]
        public IActionResult LoadSearchOrdner(OrdnerSearchModel search, int globalPageSize = 250, int pageSize = 10, int page = 1, string? SortColumn = "Name", string? SortDirection = "asc")
        {
            try
            {
                var baseQuery = _typeSearchRepo.GetFoldersSearchQuery(search, SortColumn, SortDirection);

                // Berechtigung.
                var filteredQuery = baseQuery
                    .Where(a => a.Type == "UnterOrdner")
                    .ToList()
                    .Where(a => CheckAccessSearch(Uid, "UnterOrdner", a.ObjektId))
                    .ToList();

                var totalCount = filteredQuery.Count();
                var limitedCount = Math.Min(totalCount, globalPageSize);
                page = ClampPage(page, limitedCount, pageSize);
                SaveState("Ordner", search, page, pageSize, globalPageSize, SortColumn, SortDirection);
                var paged = filteredQuery
                    .Take(globalPageSize)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new OrdnerViewModel
                {
                    Ordner = paged,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    LimitedCount = limitedCount,
                    SortColumn = SortColumn,
                    SortDirection = SortDirection
                };
                return PartialView("_OrdnerSucheTable", model);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Ordner ");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }

        }

        [HttpGet("load-search-dokument")]
        public IActionResult LoadSearchDokument(DokumentSearchModel search, int globalPageSize = 250, int pageSize = 10, int page = 1, string? SortColumn = "CreationDate", string? SortDirection = "asc")
        {
            try
            {
                var baseQuery = _typeSearchRepo.GetDokumenteSearchQuery(search, SortColumn, SortDirection);

                // Berechtigung.
                var filteredQuery = baseQuery
                    .Where(a => CheckAccessSearch(Uid, "Dokument", a.ObjektId))
                    .ToList();

                var totalCount = filteredQuery.Count();
                var limitedCount = Math.Min(totalCount, globalPageSize);
                page = ClampPage(page, limitedCount, pageSize);
                SaveState("Dokument", search, page, pageSize, globalPageSize, SortColumn, SortDirection);

                var paged = filteredQuery
                    .Take(globalPageSize)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new DokumentListViewModel
                {
                    Dokumente = paged,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    LimitedCount = limitedCount,
                    SortColumn = SortColumn,
                    SortDirection = SortDirection
                };

                return PartialView("_DokumentSucheTable", model);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Dokumente");
                TempData["FriendlyMessage"] = ex.Message;
                TempData["ErrorDetails"] = ex.ToString();

                return RedirectToAction("Index", "ExceptionsError");
            }
        }


        // Hilfsmethode, um die angeforderte Seite innerhalb der gültigen Grenzen zu halten.
        private static int ClampPage(int page, int totalCount, int pageSize)
        {
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (totalPages <= 0) return 1;
            return Math.Clamp(page, 1, totalPages);
        }


        // Speichert den aktuellen Suchzustand (Typ, Filterdaten und Pagination)
        // in der Session, damit dieser beim erneuten Laden der Seite wiederhergestellt werden kann.
        private void SaveState(string type, object data, int page, int pageSize, int globalPageSize, string? sortCol = null, string? sortDir = null)
        {
            var state = new SearchState
            {
                Type = type,
                Data = data,
                Page = page,
                PageSize = pageSize,
                GlobalPageSize = globalPageSize,
                SortColumn = sortCol,
                SortDirection = sortDir
            };
            HttpContext.Session.SetObject("Search_State", state);
        }


        //Delete Search from Session.
        [HttpGet("reset-search")]
        public IActionResult ResetSearc()
        {
            HttpContext.Session.Remove("Search_State");

            return PartialView("_SearchResult");
        }
    }
}
