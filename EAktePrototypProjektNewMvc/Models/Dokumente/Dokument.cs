namespace EAktePrototypProjektNewMvc.Models.Dokumente
{

    public class Dokument
    {
        public Guid ObjektId { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string CreatorName { get; set; }
        public DateTime CreationDate { get; set; }
        public string Modifier { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string OwnerName { get; set; }
        public string VersionLabel { get; set; }
        public string Aktenzeichen { get; set; }
        public string Gegenstand { get; set; }
        public string Antragsteller { get; set; }
        public string ContentType { get; set; }
        public bool IsHidden { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPublic { get; set; }
        public bool IsSigned { get; set; }
        public bool Final { get; set; }
        public DateTime? Eingangsdatum { get; set; }
        public DateTime? Ausgangsdatum { get; set; }
        public string LockedFrom { get; set; }
        public DateTime? LockedAt { get; set; }
        public Guid FolderId { get; set; }
        public string ACLName { get; set; }
        public bool IstAusVorlagenverwaltung { get; set; }    
        public string? Absender { get; set; } // Für Email
        public string? Empfaenger { get; set; } // Für Email

        public List<DokumentVersion> Versionen { get; set; } = new List<DokumentVersion>();
    }

    public class DokumentVersion
    {
        public string VersionNr { get; set; }
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public byte[] FileStream { get; set; }
        public string Bemerkungen { get; set; }
        public DateTime? Dokumentdatum { get; set; }
        public Guid DocumentObjektId { get; set; }
        public Guid CurrentVersionId { get; set; }
        public string ContentType { get; set; }
        public long ContentSize { get; set; }
        public long FullContentSize { get; set; }
        public bool IsSigned { get; set; }
        public bool Final { get; set; }
        public string FileExtension { get; set; }
        public string? DokumentName { get; set; }
    }
}
