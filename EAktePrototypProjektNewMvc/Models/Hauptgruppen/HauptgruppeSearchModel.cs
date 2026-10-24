namespace EAktePrototypProjektNewMvc.Models.Hauptgruppen
{
    public class HauptgruppeSearchModel
    {
        public int? GehoertZuKategorieNummer { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel { get; set; }
        public string? Beschreibung { get; set; }
        public int? HauptgruppeNummer { get; set; }
        public int? LaufenderAktenzeichenZaehler { get; set; }
        public string? ObjektId { get; set; }
    }
}
