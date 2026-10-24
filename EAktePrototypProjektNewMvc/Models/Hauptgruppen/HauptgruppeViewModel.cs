using EAktePrototypProjektNewMvc.Models.Kategorien;

namespace EAktePrototypProjektNewMvc.Models.Hauptgruppen
{
    public class HauptgruppeViewModel
    {
        public Kategorie? Kategorie { get; set; }
        public List<Hauptgruppe>? Hauptgruppen { get; set; }
        public HauptgruppeSearchModel? Search { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        public List<TableColumnDto> Columns { get; set; } = new();
    }

}
