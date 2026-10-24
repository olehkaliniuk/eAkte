namespace EAktePrototypProjektNewMvc.Models.GlobalSearch
{
    public class SearchResult
    {
        public Guid ObjectId { get; set; } 
        public string Name { get; set; } 
        public string? NodeType { get; set; }
        public byte[]? Icon { get; set; }
        public string? ColorHex { get; set; }
        public int MandantId { get; set; }
    }
}
