namespace EAktePrototypProjektNewMvc.Models.Gebuehre
{
    public class GebuehrMitTitelViewModel
    {
        public Gebuehr Gebuehr { get; set; } = new Gebuehr();
        public List<GebuehrTitel> Titel { get; set; } = new List<GebuehrTitel>();
    }
}
