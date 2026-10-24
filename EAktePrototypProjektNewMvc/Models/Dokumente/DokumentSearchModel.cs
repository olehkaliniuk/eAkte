namespace EAktePrototypProjektNewMvc.Models.Dokumente
{
    public class DokumentSearchModel
    {
        public string? Name { get; set; }
        public string? CreatorName { get; set; }

        public DateTime? CreationDateVon { get; set; }
        public DateTime? CreationDateBis { get; set; }

        public DateTime? ModifyDateVon { get; set; }
        public DateTime? ModifyDateBis { get; set; }

        public string? Subject { get; set; }
        public string? Modifier { get; set; }
        public string? VersionNr { get; set; }

        public string? Betreff { get; set; }   // NEU
        public string? Version { get; set; }

        public string? Aktenzeichen { get; set; }  // NEU
    }
}