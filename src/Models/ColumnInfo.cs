namespace Models
{
    public class ColumnInfo(string name, string dataType, int? maxLength, bool isNullable, string defaultValue)
    {
        public string Name { get; set; } = name;
        public string DataType { get; set; } = dataType;
        public int? MaxLength { get; set; } = maxLength;
        public bool IsNullable { get; set; } = isNullable;
        public string DefaultValue { get; set; } = defaultValue;
    }
}