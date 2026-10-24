using EAktePrototypProjektNewMvc.Models.Dokumente;

namespace EAktePrototypProjektNewMvc.Models.Dokumente
{
    public class DokumentViewModel
    {
        public Dokument? Dokument { get; set; }

        public List<TableColumnDto> Columns { get; set; } = new();
    }
}
