namespace EAktePrototypProjektNewMvc.Models
{
    public class VorgangStatusUebersicht
    {
        public string Aktenzeichen { get; set; }
        public string Zulassungsnummer { get; set; }

        public DateTime? Antragsbestaetigung { get; set; }
        public DateTime? SvaErgebnis { get; set; }
        public DateTime? Zwischeninformation { get; set; }
        public DateTime? UnterlagenVollstaendig { get; set; }
        public DateTime? AnlagenVollstaendig { get; set; }
        public DateTime? IOBearbeiter { get; set; }
        public DateTime? ZurUnterschrift { get; set; }
        public DateTime? Schlusszeichnung { get; set; }
        public DateTime? Reinschrift { get; set; }
        public DateTime? ZumVersand { get; set; }
        public DateTime? BescheidAnAS { get; set; }
        public DateTime? Widerspruchsfrist { get; set; }
        public DateTime? WiderspruchGebuehren { get; set; }
        public DateTime? WiderspruchZulassung { get; set; }
        public DateTime? Bestandskraeftig { get; set; }
        public DateTime? WiderspruchBearbeitet { get; set; }

        // AktiveUmlaeufeUebersicht.
        public DateTime? GeaendertAm { get; set; }
        public string GeaendertVon { get; set; }

        // Letzte offene Aktion.
        public string AktionHinweis { get; set; }
        public string AktionNachricht { get; set; }
        public string An { get; set; }
        public string AktionTitelNextId { get; set; }
    }
}
