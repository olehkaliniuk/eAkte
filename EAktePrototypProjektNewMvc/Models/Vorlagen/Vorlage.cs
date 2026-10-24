namespace EAktePrototypProjektNewMvc.Models.Vorlagen
{

    // Request to create a document from a template.
    public class DokumentAusVorlageRequest
    {
        public int VorlageVersionId { get; set; }
        public Guid FolderId { get; set; }
    }

    // Request to create a document from a template with additional information.
    public class OrdnerInfoDto
    {
        public string? SubType { get; set; }        // = Ablageklasse
        public string? Aktenzeichen { get; set; }
    }

    // Request to create a document from a template with additional information.
    public class VorlageVersionDto
    {
        public int VorlageVersionId { get; set; }
        public string VorlageName { get; set; } = string.Empty;
        public string VorlageObjektTyp { get; set; } = string.Empty;
        public bool IstTagBefuellungAktiv { get; set; }
    }

}
