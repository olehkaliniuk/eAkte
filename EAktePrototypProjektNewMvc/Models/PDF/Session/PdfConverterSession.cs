namespace EAktePrototypProjektNewMvc.Models.PDF.Session
{
    public class PdfConverterSession
    {
        public Guid TokenId { get; set; }
        public Guid DocumentObjektId { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string Name { get; set; } = null!;
    }
}
