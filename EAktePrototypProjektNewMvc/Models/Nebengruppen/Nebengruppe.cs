namespace EAktePrototypProjektNewMvc.Models.Nebengruppen
{
    public class Nebengruppe
    {
        public int Id { get; set; }
        public int GehoertZuKategorieNummer { get; set; }
        public int GehoertZuHauptgruppeNummer { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Kuerzel { get; set; }
        public string? Beschreibung { get; set; }
        public int NebengruppeNummer { get; set; }
        public Guid? ObjektId { get; set; }
        public byte[]? Icon { get; set; }
    }
}
