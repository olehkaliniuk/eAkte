namespace EAktePrototypProjektNewMvc.Models.CreateAkte.Ablaufdaten
{
    public class Ablaufdaten
    {
        public int Id { get; set; }
        public string? Aktenzeichen { get; set; }
        public DateTime? EintragAmVom { get; set; }
        public string? EintragVon { get; set; }
        public DateTime? AktionAm { get; set; }
        public string? AktionTyp { get; set; }
        public string? Bemerkung { get; set; }
    }
}
