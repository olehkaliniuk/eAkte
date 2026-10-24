namespace EAktePrototypProjektNewMvc.Models.Kategorien
{
    public class Kategorie
    {
        public int Id { get; set; }
        public int KategorieNummer { get; set; }
        public string? Bezeichnung { get; set; }
        public Guid? ObjektId { get; set; }
        public string? Beschreibung { get; set; }
        public string? Kuerzel { get; set; }
        public byte[]? Icon { get; set; }
    }
}
