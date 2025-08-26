namespace Models
{
    public class ProcedureInfo(string schemaName, string procedureName)
    {
        public string SchemaName { get; set; } = schemaName;
        public string ProcedureName { get; set; } = procedureName;
        public List<ParameterInfo>? Parameters { get; set; } = null;
        public string Definition { get; set; } = string.Empty;
    }
}