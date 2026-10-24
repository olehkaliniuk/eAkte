using DreiF.Shared.Abstractions;
using EAktePrototypProjektNewMvc.Models.Adressen;
using EAktePrototypProjektNewMvc.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EAktePrototypProjektNewMvc.Controllers
{
    [Route("address")]
    public class AdresseController : Controller
    {
        private readonly AdresseRepository _repo;
        private readonly ILogger<HomeController> _logger;

        public AdresseController(IConfiguration config, ILogger<HomeController> logger, ICurrentMandantProvider mandantProvider)
        {
            _repo = new AdresseRepository(config.GetConnectionString("DefaultConnection"), mandantProvider);
            _logger = logger;
        }

        [HttpGet("load-address")]
        public IActionResult LoadAdressen(AdresseSearchModel search, int page = 1, int pageSize = 10, string sortColumn = "Id", string sortDirection = "asc")
        {
            try
            {
                var all = _repo.GetAdresses();

                if (search.Id > 0)
                {
                    all = all.Where(a => a.Id == search.Id).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Land))
                {
                    all = all.Where(a => a.Land != null &&
                        a.Land.Contains(search.Land, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Strasse))
                {
                    all = all.Where(a => a.Strasse != null &&
                        a.Strasse.Contains(search.Strasse, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Ort))
                {
                    all = all.Where(a => a.Ort != null &&
                        a.Ort.Contains(search.Ort, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.StateOrPrefecture))
                {
                    all = all.Where(a => a.StateOrPrefecture != null &&
                        a.StateOrPrefecture.Contains(search.StateOrPrefecture, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.PLZ))
                {
                    all = all.Where(a => a.PLZ != null &&
                        a.PLZ.Contains(search.PLZ, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Gebaude))
                {
                    all = all.Where(a => a.Gebaude != null &&
                        a.Gebaude.Contains(search.Gebaude, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Wohnung))
                {
                    all = all.Where(a => a.Wohnung != null &&
                        a.Wohnung.Contains(search.Wohnung, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Organisation))
                {
                    all = all.Where(a => a.Organisation != null &&
                        a.Organisation.Contains(search.Organisation, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    all = all.Where(a => a.Name != null &&
                        a.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Bezeichnung))
                {
                    all = all.Where(a => a.Bezeichnung != null &&
                        a.Bezeichnung.Contains(search.Bezeichnung, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Iban))
                {
                    all = all.Where(a => a.Iban != null &&
                        a.Iban.Contains(search.Iban, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Bic))
                {
                    all = all.Where(a => a.Bic != null &&
                        a.Bic.Contains(search.Bic, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Ansprechpartner))
                {
                    all = all.Where(a => a.Ansprechpartner != null &&
                        a.Ansprechpartner.Contains(search.Ansprechpartner, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.AnsprechpartnerTel))
                {
                    all = all.Where(a => a.AnsprechpartnerTel != null &&
                        a.AnsprechpartnerTel.Contains(search.AnsprechpartnerTel, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.AnsprechpartnerEmail))
                {
                    all = all.Where(a => a.AnsprechpartnerEmail != null &&
                        a.AnsprechpartnerEmail.Contains(search.AnsprechpartnerEmail, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.RechnungsOrt))
                {
                    all = all.Where(a => a.RechnungsOrt != null &&
                        a.RechnungsOrt.Contains(search.RechnungsOrt, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.RechnungsLand))
                {
                    all = all.Where(a => a.RechnungsLand != null &&
                        a.RechnungsLand.Contains(search.RechnungsLand, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Gesamtadresse))
                {
                    all = all.Where(a => a.Gesamtadresse != null &&
                        a.Gesamtadresse.Contains(search.Gesamtadresse, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(search.Fax))
                {
                    all = all.Where(a => a.Fax != null &&
                        a.Fax.Contains(search.Fax, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                //Bool.
                if (search.IstInsolvent.HasValue)
                {
                    all = all.Where(a => a.IstInsolvent == search.IstInsolvent.Value).ToList();
                }

                if (search.IstAktiv.HasValue)
                {
                    all = all.Where(a => a.IstAktiv == search.IstAktiv.Value).ToList();
                }

                // --------------------
                // SORTING
                // --------------------

                var sortMap = new Dictionary<string, Func<Adresse, object>>
                {
                    { "Id", a => a.Id},
                    { "Land", a => a.Land },
                    { "Strasse", a => a.Strasse },
                    { "Ort", a => a.Ort },
                    { "StateOrPrefecture", a => a.StateOrPrefecture },
                    { "PLZ", a => a.PLZ },
                    { "Gebaude", a => a.Gebaude },
                    { "Wohnung", a => a.Wohnung },
                    { "Organisation", a => a.Organisation },
                    { "Name", a => a.Name },
                    { "Bezeichnung", a => a.Bezeichnung },
                    { "Iban", a => a.Iban },
                    { "Bic", a => a.Bic },
                    { "IstInsolvent", a => a.IstInsolvent ?? false },
                    { "IstAktiv", a => a.IstAktiv ?? false },
                    { "Ansprechpartner", a => a.Ansprechpartner },
                    { "AnsprechpartnerTel", a => a.AnsprechpartnerTel },
                    { "AnsprechpartnerEmail", a => a.AnsprechpartnerEmail },
                    { "RechnungsStrasse", a => a.RechnungsStrasse },
                    { "RechnungsOrt", a => a.RechnungsOrt },
                    { "RechnungsPLZ", a => a.RechnungsPLZ },
                    { "RechnungsLand", a => a.RechnungsLand },
                    { "RechnungsStateOrPrefecture", a => a.RechnungsStateOrPrefecture },
                    { "RechnungsOrganisation", a => a.RechnungsOrganisation },
                    { "RechnungsName", a => a.RechnungsName },
                    { "Gesamtadresse", a => a.Gesamtadresse },
                    { "Fax", a => a.Fax }
                };

                if (!sortMap.ContainsKey(sortColumn))
                {
                    sortColumn = "Id";
                }

                all = sortDirection == "asc"
                    ? all.OrderBy(sortMap[sortColumn]).ToList()
                    : all.OrderByDescending(sortMap[sortColumn]).ToList();

                var totalCount = all.Count;

                var paged = all
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var model = new AdresseViewModel
                {
                    Adressen = paged,
                    Search = search,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    SortColumn = sortColumn,
                    SortDirection = sortDirection

                };

                return PartialView("_AdresseModal", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Adressen");
                return StatusCode(500);
            }

        }
    }
}