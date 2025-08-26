namespace Models
{
    public class ForeignKeyInfo(string name, string columnName, string referencedSchema, string referencedTable, string referencedColumn)
    {
        public string Name { get; set; } = name;
        public string ColumnName { get; set; } = columnName;
        public string ReferencedSchema { get; set; } = referencedSchema;
        public string ReferencedTable { get; set; } = referencedTable;
        public string ReferencedColumn { get; set; } = referencedColumn;
    }
}