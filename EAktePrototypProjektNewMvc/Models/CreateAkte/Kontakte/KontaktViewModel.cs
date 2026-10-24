namespace EAktePrototypProjektNewMvc.Models.CreateAkte.Kontakte
{
    public class KontaktViewModel
    {
        public List<Kontakt> Kontakte { get; set; } = new();
        public KontaktSearchModel Search { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string SortColumn { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";
    }
}
