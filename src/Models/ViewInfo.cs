namespace Models
{
    public class ViewInfo(string schemaName, string viewName)
    {
        public string SchemaName { get; set; } = schemaName;
        public string ViewName { get; set; } = viewName;
        public List<ColumnInfo>? Columns { get; set; } = null;
        public string Definition { get; set; } = string.Empty;
    }
}