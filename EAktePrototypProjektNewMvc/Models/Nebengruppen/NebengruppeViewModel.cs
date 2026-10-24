using EAktePrototypProjektNewMvc.Models.Kategorien;
using EAktePrototypProjektNewMvc.Models.Hauptgruppen;

namespace EAktePrototypProjektNewMvc.Models.Nebengruppen
{
    public class NebengruppeViewModel
    {
        public Kategorie? Kategorie { get; set; }
        public Hauptgruppe? Hauptgruppe { get; set; }
        public List<Nebengruppe>? Nebengruppen { get; set; }
        public NebengruppeSearchModel? Search { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        public List<TableColumnDto> Columns { get; set; } = new();
    }
}
