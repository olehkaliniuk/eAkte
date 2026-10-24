namespace EAktePrototypProjektNewMvc.Models.Vorgange
{
    public class VorgangSearchModel
    {
        public string? Aktenzeichen { get; set; }
        public string? UserID { get; set; }
        public string? Abteilung { get; set; }
        public string? Stelle { get; set; }
        public string? Zulassungsnummer { get; set; }
        public DateTime? DatumAntragVon { get; set; }
        public DateTime? DatumAntragBis { get; set; }
        public string? Antragsart { get; set; }
        public string? Bescheidtyp { get; set; }
        public string? Gegenstand { get; set; }
        public string? Status { get; set; }
        public string? Betreff { get; set; }
        public string? Bemerkung { get; set; }
        public Guid? ObjektId { get; set; }
        public string? Aktenzustand { get; set; }
        public string? ErzeugtVon { get; set; }
        public DateTime? ErzeugtAm { get; set; }
        public DateTime? ErzeugtAmVon { get; set; }
        public DateTime? ErzeugtAmBis { get; set; }
        public bool? Veroeffentlicht { get; set; }
    }
}
