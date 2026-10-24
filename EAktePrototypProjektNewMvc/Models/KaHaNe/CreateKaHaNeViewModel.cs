using EAktePrototypProjektNewMvc.Models.Kategorien;
using EAktePrototypProjektNewMvc.Models.Hauptgruppen;
using EAktePrototypProjektNewMvc.Models.Nebengruppen;

namespace EAktePrototypProjektNewMvc.Models.KaHaNe
{
    public class CreateKaHaNeViewModel
    {
        public string ObjectType { get; set; } = "";   // "kategorie" | "hauptgruppe" | "nebengruppe"
        public string Mode { get; set; } = "";          // "create" | "edit"
        public Guid ObjectId { get; set; }
        public Guid ParentObjectId { get; set; }
        public string? ParentBezeichnung { get; set; }
        public byte[]? ParentIcon { get; set; }

        public List<string> Abteilungen { get; set; } = new();           // Kategorie: alle; Hauptgruppe: nur die der Elternkategorie (Anzeige, nicht wählbar)
        public List<string> SelectedAbteilungen { get; set; } = new();   // Kategorie edit

        public List<ReferatDto> Referate { get; set; } = new();                        // Hauptgruppe: alle Referate der Abteilungen
        public List<AbteilungReferatDto> SelectedAbteilungReferate { get; set; } = new(); // Hauptgruppe edit: ausgewählte Referate

        public Kategorie? Kategorie { get; set; }
        public Hauptgruppe? Hauptgruppe { get; set; }
        public Nebengruppe? Nebengruppe { get; set; }
    }


    public class ReferatDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AbteilungName { get; set; }
    }

    public class AbteilungReferatDto
    {
        public string Abteilung { get; set; }
        public string Referat { get; set; }
    }
}