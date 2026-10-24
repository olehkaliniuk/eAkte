namespace EAktePrototypProjektNewMvc.Models.TypeSearch
{
    public class SearchState
    {
        public string? Type { get; set; }
        public object? Data { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int GlobalPageSize { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
    }
}
