namespace EAktePrototypProjektNewMvc.Models.CreateAkte
{
    public class Vorgangstyp
    {
        public int Id { get; set; }
        public string? Bereich { get; set; }
        public string? Kennzeichen { get; set; }
        public string? Type { get; set; }
        public string? TypeFuerBescheidzuordnung { get; set; }
        public int Hauptgruppe { get; set; }
        public int Aktenzeichenkreis { get; set; }
        public decimal ReisekostenInProzent { get; set; }
        public bool Veroeffentlichen { get; set; }
        public bool Bestandskraftsetzen { get; set; }
        public string? Vorgangsdatenanwendung { get; set; }
        public string? TypeFuerMakro { get; set; }
    }
}
