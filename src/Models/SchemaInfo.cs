namespace Models
{
    public class SchemaInfo(string schemaName)
    {
        public string SchemaName { get; set; } = schemaName;
        public int Tables { get; set; }
        public int Views { get; set; }
        public int StoredProcedures { get; set; }
    }
}