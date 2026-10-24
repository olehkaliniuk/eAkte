namespace EAktePrototypProjektNewMvc.Models.Ordner
{
    public class OrdnerDokumentRow
    {
        public Guid? ObjektId { get; set; }
        public Guid? ParentObjektId { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Bemerkungen { get; set; }
        public string? CreatorName { get; set; }
        public DateTime? CreationDate { get; set; }
        public bool IsFolder { get; set; }
        // Ordner fields. 
        public DateTime? ModifyDate { get; set; }
        public string? Modifier { get; set; }
        public string? OwnerName { get; set; }
        public string? Hauptsachgebiet { get; set; }
        public string? Sachgebiet { get; set; }
        public string? Aktenzeichen { get; set; }
        public int? LfdNr { get; set; }
        public string? Status { get; set; }
        public bool? Final { get; set; }
        public string? ColorHex { get; set; }
        // Dokument fields.
        public Guid? VersionId { get; set; }
        public string? FileExtension { get; set; }
        public long? FileSize { get; set; }
        public string? ContentType { get; set; }
        public bool IstAusVorlagenverwaltung { get; set; }
        public string? FileExt { get; set; }
        public string? Version { get; set; } 
        public string? Betreff { get; set; }
        public string? LockedFrom { get; set; }
        public DateTime? LockedAt { get; set; }

        public string? Absender { get; set; } // Für Email
        public string? Empfaenger { get; set; } // Für Email

    }
}
