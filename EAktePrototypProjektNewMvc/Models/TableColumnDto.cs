namespace EAktePrototypProjektNewMvc.Models
{
    public class TableColumnDto
    {
        public string ColumnKey { get; set; }
        public string Title { get; set; }
        public int SortOrder { get; set; }
        public bool IsVisible { get; set; }
        public bool IsSortable { get; set; }
        public bool IsSearchable { get; set; }
    }

    // Kommt vom Client: Reihenfolge = Position in der Liste.
    public class UserColumnSetting
    {
        public string ColumnKey { get; set; }
        public bool IsVisible { get; set; }
    }

    // Für den Dialog.
    public class ColumnEditDto
    {
        public string ColumnKey { get; set; }
        public string Title { get; set; }
        public bool IsVisible { get; set; }
    }
}
