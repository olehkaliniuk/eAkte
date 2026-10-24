using EAktePrototypProjektNewMvc.Models.Hauptgruppen;
using EAktePrototypProjektNewMvc.Models.Nebengruppen;

namespace EAktePrototypProjektNewMvc.Models.Akten
{
    public class AkteViewModel
    {
        public Hauptgruppe? Hauptgruppe { get; set; }
        public Nebengruppe? Nebengruppe { get; set; }
        public List<Akte>? Akte { get; set; }
        public AkteSearchModel? Search { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int LimitedCount { get; set; } // Für Type-Search.
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public int TotalPagesSearch => (int)Math.Ceiling(LimitedCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }


        public List<TableColumnDto> Columns { get; set; } = new();
    }
}
