using EAktePrototypProjektNewMvc.Models.Dokumente;
using EAktePrototypProjektNewMvc.Models.Vorgange;

namespace EAktePrototypProjektNewMvc.Models.Ordner
{
    public class OrdnerViewModel
    {
        public Vorgang? Vorgang { get; set; }
        public Ordner? Parentordner { get; set; }
        public List<Dokument>? Dokumente { get; set; }
        public List<Ordner>? Ordner { get; set; }
        public OrdnerSearchModel? Search { get; set; }
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
