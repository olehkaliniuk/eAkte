namespace EAktePrototypProjektNewMvc.Models.KaHaNe
{
    public class SaveKaHaNeRequest
    {
        public string ObjectType { get; set; } = "";
        public Guid ObjectId { get; set; }
        public Guid ParentObjectId { get; set; }
        public string Bezeichnung { get; set; } = "";
        public string Kuerzel { get; set; } = "";
        public string Beschreibung { get; set; } = "";
        public string IconAction { get; set; } = "keep"; // "keep" | "new" | "remove"
        public string? IconBase64 { get; set; }

        public List<string>? Abteilungen { get; set; } = new(); // Für Kategorie.
        public List<int> Referate { get; set; } = new(); // Für Hauptgruppe.
    }
}
