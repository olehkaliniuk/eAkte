namespace EAktePrototypProjektNewMvc.Models.CreateAkte.Ablaufdaten
{
    public class AblaufdatenViewModel
    {
        public List<Ablaufdaten> Ablaufdaten { get; set; } = new();
        public AblaufdatenSearchModel Search { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string SortColumn { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";
        public List<Aktionstypen> Aktionstypen { get; set; } = new();
    }
}
