namespace EAktePrototypProjektNewMvc.Models.Vorgange
{
    public class Vorgang
    {
        public string? Aktenzeichen { get; set; }
        public string? UserID { get; set; }
        public string? Stelle { get; set; }
        public string? Zulassungsnummer { get; set; }
        public DateTime? DatumAntrag { get; set; }
        public string? Antragsart { get; set; }
        public string? Bescheidtyp { get; set; }
        public string? Gegenstand { get; set; }
        public string? Status { get; set; }
        public string? Betreff { get; set; }
        public string? Bemerkung { get; set; }
        public Guid ObjektId { get; set; }
        public string? Aktenzustand { get; set; }
        public string? ErzeugtVon { get; set; }
        public DateTime? ErzeugtAm { get; set; }
        public bool Veroeffentlicht { get; set; }
        public byte[]? Icon { get; set; }
        public string? Abteilung { get; set; }
        public string? Referat { get; set; }
        public bool AntragVorhanden { get; set; }
        public bool BescheidNeufassung { get; set; }
        public string Ansprechpartner { get; set; }

        //UpdateVorgang Request.
        // Gehört zur Akte, nicht zum Vorgang.
        public string GeandertVon { get; set; }
        public DateTime? GeandertAm { get; set; }

    }
}