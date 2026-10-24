namespace EAktePrototypProjektNewMvc.Models.Akten
{
    public class Akte
    {
        public string? Zulassungsnummer { get; set; }
        public int? LfdNr { get; set; }
        public int? HauptgruppeNummer { get; set; }
        public int? NebengruppeNummer { get; set; }
        public Guid ObjektId { get; set; }
        public string? Zustand { get; set; }
        public string? ErzeugtVon { get; set; }
        public DateTime? ErzeugtAm { get; set; }
        public string? GeandertVon { get; set; }
        public DateTime? GeandertAm { get; set; }
        public byte[]? Icon { get; set; }
        public string? Abteilung { get; set; }
        public string? Referat { get; set; }
    }
}