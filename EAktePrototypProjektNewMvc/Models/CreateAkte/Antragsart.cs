namespace EAktePrototypProjektNewMvc.Models.CreateAkte
{
    public class Antragsart
    {
        public int Id { get; set; }
        public string? Bereich { get; set; }
        public string? Kennzeichen { get; set; }
        public int Type { get; set; }
        public string? Bezeichnung { get; set; }
        public int? Tarifstelle { get; set; }
        public int? Aktenzeichenkreis { get; set; }
    }
}
