namespace EAktePrototypProjektNewMvc.Models.CreateAkte
{
    public class Aktionstypen
    {
        public int Id { get; set; }
        public int? ParameterId { get; set; }
        public string ParameterTyp { get; set; } = string.Empty;
        public string? ParameterKey { get; set; }
        public string? ParameterWertString { get; set; }
        public string? ParameterWertString2 { get; set; }
        public int? ParameterWertInt { get; set; }
        public bool? ParameterWertBool { get; set; }
        public decimal? ParameterWertDecimal { get; set; }
        public DateTime? ParameterDate { get; set; }
        public string? ParameterBeschreibung { get; set; }
        public string? ZugriffNurFuerUserid { get; set; }
    }
}
