namespace EAktePrototypProjektNewMvc.Models.Persone
{
    public class PersonRolle
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string? Rolle { get; set; }
        public string? Abteilung { get; set; }
        public string? Referat { get; set; }
        public DateTime? GueltigAb { get; set; }
        public DateTime? GueltigBis { get; set; }
        public DateTime? AngelegtAm { get; set; }
    }
}