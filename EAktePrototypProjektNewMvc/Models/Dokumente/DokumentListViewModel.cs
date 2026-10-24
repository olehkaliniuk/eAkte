namespace EAktePrototypProjektNewMvc.Models.Dokumente
{
    public class DokumentListViewModel
    {
        public List<DokumentListItem>? Dokumente { get; set; }
        public DokumentSearchModel? Search { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        public int LimitedCount { get; set; } // Für Type-Search.
        // Wie viele Seiten es innerhalb des globalPageSize-Limits gibt.
        public int TotalPagesSearch => (int)Math.Ceiling(LimitedCount / (double)PageSize);


        // NEU: für die Dokumentenübersicht
        public Guid ObjectId { get; set; }
        public string? NodeType { get; set; }

        public string Table { get; set; }

        public List<TableColumnDto> Columns { get; set; } = new();
    }

    // Flaches Modell für die Tabellenzeile.
    public class DokumentListItem
    {
        public Guid ObjektId { get; set; }

        // Sichtbare Spalten (wichtigste Felder).
        public string? Name { get; set; }
        public string? ContentType { get; set; }
        public string? CreatorName { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string? VersionNr { get; set; }

        public Guid? VersionId { get; set; }     // NEU — echte ObjektId der Version
        public string? LockedFrom { get; set; }

        // Nur in der Info-Ansicht (per Klick).
        public string? Betreff { get; set; }   // NEU
        public string? Version { get; set; }
        public string? Subject { get; set; }
        public string? Gegenstand { get; set; }
        public string? Antragsteller { get; set; }
        public string? OwnerName { get; set; }
        public string? Aktenzeichen { get; set; }
        public string? ACLName { get; set; }
        public string? Modifier { get; set; }
        public DateTime? Eingangsdatum { get; set; }
        public bool IsPublic { get; set; }
        public bool IsSigned { get; set; }
        public bool Final { get; set; }
        public bool IsHidden { get; set; }
        public bool IsDeleted { get; set; }

        // Aus aktiver Version.
        public string? Description { get; set; }
        public string? Bemerkungen { get; set; }
        public string? FileExtension { get; set; }
        public long ContentSize { get; set; }
        public string? VersionCreatedBy { get; set; }
        public DateTime? Dokumentdatum { get; set; }

        public bool IstAusVorlagenverwaltung { get; set; }

        public string? Absender { get; set; } // Für Email
        public string? Empfaenger { get; set; } // Für Email
    }
}