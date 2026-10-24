namespace EAktePrototypProjektNewMvc.Models.Tree
{
    public class TreeNode
    {
        // Current.
        public Guid? ObjectId { get; set; }
        // Parent.
        public Guid? ParentObjectId { get; set; }
        public string? Title { get; set; }
        public string? NodeType { get; set; }
        public string? ParentNodeType { get; set; }
        public bool HasChildren { get; set; }
        public string? ColorHex { get; set; }
        public byte[]? Icon { get; set; }
    }
}
