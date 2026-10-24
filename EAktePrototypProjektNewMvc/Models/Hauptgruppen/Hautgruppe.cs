namespace EAktePrototypProjektNewMvc.Models.Hauptgruppen
{
    public class Hauptgruppe
    {
        public int Id { get; set; }
        public int GehoertZuKategorieNummer { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel { get; set; }
        public string? Beschreibung { get; set; }
        public int HauptgruppeNummer { get; set; }
        public int LaufenderAktenzeichenZaehler { get; set; }
        public Guid? ObjektId { get; set; }
        public byte[]? Icon { get; set; }
    }
}
