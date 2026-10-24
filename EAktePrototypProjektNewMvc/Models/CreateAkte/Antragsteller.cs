namespace EAktePrototypProjektNewMvc.Models
{
    public class Antragsteller
    {
        public int Id { get; set; }
        public string? Aktenzeichen { get; set; }
        public string? Type { get; set; }        
        public string? Adresse { get; set; }
        public string? AdresseZusatz { get; set; }
        public string? Email { get; set; }
        public string? Tel { get; set; }
        public string? Fax { get; set; }
        public bool Aktiv { get; set; }
    }
}
