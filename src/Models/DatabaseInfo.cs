namespace Models
{
    public class DatabaseInfo
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime LastConnected { get; set; }
        public bool IsConnected { get; set; }
    }
}