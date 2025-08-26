namespace Models
{
    public class ParameterInfo(string? name, string mode, string dataType, int? maxLength, string defaultValue)
    {
        public string? Name { get; set; } = name;
        public string Mode { get; set; } = mode;
        public string DataType { get; set; } = dataType;
        public int? MaxLength { get; set; } = maxLength;
        public string DefaultValue { get; set; } = defaultValue;
    }
}