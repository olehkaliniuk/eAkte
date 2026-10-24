using EAktePrototypProjektNewMvc.Models.Akten;

namespace EAktePrototypProjektNewMvc.Models.Vorgange
{
    public class VorgangViewModel
    {
        public Akte? Akte { get; set; }
        public List<Vorgang>? Vorgang { get; set; }
        public VorgangSearchModel? Search { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int LimitedCount { get; set; } // Für Type-Search.
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public int TotalPagesSearch => (int)Math.Ceiling(LimitedCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        public List<TableColumnDto> Columns { get; set; } = new();

        public string ViewMode { get; set; } = "Standard"; // "Standard" | "Mitzeichnung" | "MeineVorgaenge"
    }
}
