namespace EAktePrototypProjektNewMvc.Models.Gebuehre
{
    public class Gebuehr
    {
        public string Aktenzeichen { get; set; } = string.Empty;
        public int LfdNr { get; set; }
        public string? Kassenzeichen { get; set; }
        public string? Typ { get; set; }
        public string? Status { get; set; }
        public string? Tarifstelle { get; set; }
        public string? Ermittlung { get; set; }
        public string? Bemerkung { get; set; }
        public string? IBAN { get; set; }
        public string? BIC { get; set; }
        public string? GebuehrenbescheidAn { get; set; }
        public decimal? Gebuehr_ { get; set; }   // Spalte [Gebuehr]
        public decimal? GezahlteGebuehr { get; set; }
        public DateTime? BeantragtAm { get; set; }
        public DateTime? GebuehrGezahltAm { get; set; }
        public DateTime? AnnahmeanordnungVom { get; set; }
        public DateTime? GebuehrenbescheidVom { get; set; }
        public DateTime? KassenwirksamAm { get; set; }
        public DateTime? StorniertAm { get; set; }
        public bool SollStorniertWerden { get; set; }
        public string? AngelegtVon { get; set; }
        public DateTime? AngelegtAm { get; set; }
        public string? GeaendertVon { get; set; }
        public DateTime? GeaendertAm { get; set; }
        public string? Referenznummer { get; set; }
        public string? Belegnummer { get; set; }
        public DateTime? UebergabeAm { get; set; }
        public string? UebergabeStelle { get; set; }
        public DateTime? EingelesenAm { get; set; }
        public string? StornoStelle { get; set; }
        public string? StatusProzess { get; set; }
        public bool KassenzeichenSollGeaendertWerden { get; set; }
        public int? LfdNrAnnahmeanordnung { get; set; }
        public int? LfdNrGebuehrenbescheid { get; set; }
        public string? AnnahmeanordnungBefugter { get; set; }
        public string? UnterschriebenVon { get; set; }
        public string? FreigegebenVon { get; set; }
    }

    public class GebuehrNeuTitel
    {
        public string TitelNr { get; set; } = string.Empty;
        public decimal Betrag { get; set; }
    }

    public class GebuehrNeuRequest
    {
        public string Aktenzeichen { get; set; } = string.Empty;
        public string Art { get; set; } = string.Empty;      // "V" oder "G"
        public string? Tarifstelle { get; set; }
        public string? Ermittlung { get; set; }
        public string? Bemerkung { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
        public List<GebuehrNeuTitel> Titel { get; set; } = new();
    }


    public class GebuehrEditRequest
    {
        public string Aktenzeichen { get; set; } = string.Empty;
        public int LfdNr { get; set; }
        public string? Tarifstelle { get; set; }
        public string? Ermittlung { get; set; }
        public string? Bemerkung { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
        public List<GebuehrNeuTitel> Titel { get; set; } = new();
    }


    public class GebuehrStatusRequest
    {
        public string Aktenzeichen { get; set; } = string.Empty;
        public int LfdNr { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
