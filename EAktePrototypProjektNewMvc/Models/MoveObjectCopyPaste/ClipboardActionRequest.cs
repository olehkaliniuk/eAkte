namespace EAktePrototypProjektNewMvc.Models.MoveObjectCopyPaste
{
    // Copy / Paste / Move
    public class ClipboardActionRequest
    {
        public string TargetObjectId { get; set; }
        public List<string> SourceObjectIds { get; set; }
        public string SourceType { get; set; }
    }
}
