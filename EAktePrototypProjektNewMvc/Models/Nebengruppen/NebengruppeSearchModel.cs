namespace EAktePrototypProjektNewMvc.Models.Nebengruppen
{
    public class NebengruppeSearchModel
    {
        public int? GehoertZuKategorieNummer { get; set; }
        public int? GehoertZuHauptgruppeNummer { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel { get; set; }
        public string? Beschreibung { get; set; }
        public int? NebengruppeNummer { get; set; }
        public string? ObjektId { get; set; }
    }
}
