namespace EAktePrototypProjektNewMvc.Models.Adressen
{
    public class AdresseViewModel
    {
        public List<Adresse>? Adressen { get; set; }
        public AdresseSearchModel? Search {  get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }
}
