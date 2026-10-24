namespace EAktePrototypProjektNewMvc.Models.Dokumente.Sessions
{
    // DokumentEditSession repräsentiert eine Bearbeitungssitzung für ein Dokument. 
    public class DokumentEditSession
    {
        public Guid TokenId { get; set; }
        public Guid DocumentObjektId { get; set; }
        public Guid OriginalVersionId { get; set; }
        public string UserId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string? ContentType { get; set; }
        public byte[]? StagedFileStream { get; set; }
        public bool HasPendingChanges { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string? LockToken { get; set; }
        public int MandantId { get; set; }
        public bool IsViewOnly { get; set; }
    }

    // Plugin Request.
    public class ConfirmEditRequestDto
    {
        public Guid Token { get; set; }
        public string DocumentBytes { get; set; }
    }
}
