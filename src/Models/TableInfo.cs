namespace Models
{
    public class TableInfo(string schemaName, string tableName)
    {
        public string SchemaName { get; set; } = schemaName;
        public string TableName { get; set; } = tableName;
        public List<ColumnInfo>? Columns { get; set; } = null;
        public List<string>? PrimaryKeys { get; set; } = null;
        public List<ForeignKeyInfo>? ForeignKeys { get; set;} = null;
    }
}