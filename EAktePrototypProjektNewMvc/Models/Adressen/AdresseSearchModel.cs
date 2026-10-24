namespace EAktePrototypProjektNewMvc.Models.Adressen
{
    public class AdresseSearchModel
    {
        public int? Id { get; set; }
        public string? Land { get; set; }
        public string? Strasse { get; set; }
        public string? Ort { get; set; }
        public string? StateOrPrefecture { get; set; }
        public string? PLZ { get; set; }
        public string? Gebaude { get; set; }
        public string? Wohnung { get; set; }
        public string? Organisation { get; set; }
        public string? Name { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
        public bool? IstInsolvent { get; set; }
        public bool? IstAktiv { get; set; }
        public string? Ansprechpartner { get; set; }
        public string? AnsprechpartnerTel { get; set; }
        public string? AnsprechpartnerEmail { get; set; }
        public string? RechnungsStrasse { get; set; }
        public string? RechnungsOrt { get; set; }
        public string? RechnungsPLZ { get; set; }
        public string? RechnungsLand { get; set; }
        public string? RechnungsStateOrPrefecture { get; set; }
        public string? RechnungsOrganisation { get; set; }
        public string? RechnungsName { get; set; }
        public string? Gesamtadresse { get; set; }
        public string? Fax { get; set; }
    }
}
