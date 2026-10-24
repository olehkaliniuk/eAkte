namespace EAktePrototypProjektNewMvc.Models.Ordner
{
    public class OrdnerSearchModel
    {
        public Guid? ObjektId { get; set; }
        public Guid? ParentObjectId { get; set; }
        public string? Name { get; set; } = string.Empty;
        public DateTime? CreationDate { get; set; }
        public DateTime? CreationDateVon { get; set; }
        public DateTime? CreationDateBis { get; set; }
        public string? Type { get; set; }
        public string? CreatorName { get; set; }
        public string? Modifier { get; set; }
        public DateTime? ModifyDateVon { get; set; }
        public DateTime? ModifyDateBis { get; set; }
        public string? OwnerName { get; set; }
        public int? OwnerPermit { get; set; }
        public int? GroupPermit { get; set; }
        public int? WorldPermit { get; set; }
        public string? Hauptsachgebiet { get; set; }
        public string? Sachgebiet { get; set; }
        public string? Aktenzeichen { get; set; }
        public int? LfdNr { get; set; }
        public string? Bemerkungen { get; set; }
        public string? Status { get; set; }
        public bool? Final { get; set; }
        public string? Path { get; set; }

        public string? Betreff { get; set; }
        public string? Version { get; set; }
    }
}
