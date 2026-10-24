namespace EAktePrototypProjektNewMvc.Models.Kategorien
{
    public class KategorieViewModel
    {
        public List<Kategorie>? Kategorien { get; set; }
        public KategorieSearchModel? Search { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        public List<TableColumnDto> Columns { get; set; } = new();
    }
}
