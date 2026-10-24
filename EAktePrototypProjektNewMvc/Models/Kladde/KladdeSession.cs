namespace EAktePrototypProjektNewMvc.Models.Kladde
{
    public class KladdeSession
    {
        public Guid TokenId { get; set; }
        public string Uid { get; set; } = string.Empty;
        public DateTime? IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? LockToken { get; set; } = string.Empty;
    }
}
