namespace EAktePrototypProjektNewMvc.Models.Tree
{
    // View.
    public class TreeItem
    {
        public Guid ObjectId { get; set; }
        public Guid? ParentObjectId { get; set; }
        public string? Title { get; set; }
        public string? Url { get; set; }
        public string? NodeType { get; set; }
        public bool IsExpanded { get; set; }
        public List<TreeItem>? Children { get; set; }
        public bool HasChildren { get; set; }
        public string? ColorHex { get; set; }
        public byte[]? Icon { get; set; }
    }
}
