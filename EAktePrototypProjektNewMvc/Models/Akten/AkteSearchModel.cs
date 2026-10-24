namespace EAktePrototypProjektNewMvc.Models.Akten
{
    public class AkteSearchModel
    {
        public string? Zulassungsnummer { get; set; }
        public int? LfdNr { get; set; }
        public string? HauptgruppeNummer { get; set; }
        public string? NebengruppeNummer { get; set; }
        public Guid? ObjektId { get; set; }
        public string? Zustand { get; set; }
        public string? ErzeugtVon { get; set; }
        public DateTime? ErzeugtAm { get; set; }
        public DateTime? ErzeugtAmVon { get; set; }
        public DateTime? ErzeugtAmBis { get; set; }
        public string? GeandertVon { get; set; }
        public DateTime? GeandertAm { get; set; }
        public DateTime? GeandertAmVon { get; set; }
        public DateTime? GeandertAmBis { get; set; }


    }

}
