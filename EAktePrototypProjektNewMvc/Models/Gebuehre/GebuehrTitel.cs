namespace EAktePrototypProjektNewMvc.Models.Gebuehre
{
    public class GebuehrTitel
    {
        public string Aktenzeichen { get; set; } = string.Empty;
        public int LfdNr { get; set; }
        public string? TitelNr { get; set; }
        public decimal? Betrag { get; set; }
        public decimal? BetragBezahlt { get; set; }
        public string? Kassenzeichen { get; set; }
        public string? Typ { get; set; }
    }
}
