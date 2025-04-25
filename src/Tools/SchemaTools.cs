using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using SqlServerMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace SqlServerMcpServer.Tools
{
    [McpServerToolType]
    public class SchemaTools
    {
        private readonly SqlConnectionFactory _connectionFactory;
        
        public SchemaTools(SqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [McpServerTool, Description("Lists all tables in the database.")]
        public async Task<string> ListTables(CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60); // Longer timeout for schema operations
            await connection.OpenAsync(cancellationToken);

            var tables = new List<string>();
            
            // Query to get all user tables
            string query = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' 
                ORDER BY TABLE_NAME";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(reader.GetString(0));
            }

            return JsonSerializer.Serialize(tables, _jsonOptions);
        }

        [McpServerTool, Description("Gets the schema of a specific table.")]
        public async Task<string> GetTableSchema(
            [Description("The name of the table")] string tableName,
            CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            var columns = new List<Dictionary<string, string>>();
            
            // Query to get column information
            string query = @"
                SELECT 
                    COLUMN_NAME, 
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    IS_NULLABLE,
                    COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var column = new Dictionary<string, string>
                {
                    ["name"] = reader.GetString(0),
                    ["dataType"] = reader.GetString(1),
                    ["maxLength"] = reader.IsDBNull(2) ? "null" : reader.GetInt32(2).ToString(),
                    ["isNullable"] = reader.GetString(3),
                    ["defaultValue"] = reader.IsDBNull(4) ? "null" : reader.GetString(4)
                };
                
                columns.Add(column);
            }

            return JsonSerializer.Serialize(columns, _jsonOptions);
        }

        [McpServerTool, Description("Lists all views in the database.")]
        public async Task<string> ListViews(CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            var views = new List<string>();
            
            // Query to get all views
            string query = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.VIEWS 
                ORDER BY TABLE_NAME";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                views.Add(reader.GetString(0));
            }

            return JsonSerializer.Serialize(views, _jsonOptions);
        }

        [McpServerTool, Description("Lists all stored procedures in the database.")]
        public async Task<string> ListStoredProcedures(CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            var procedures = new List<string>();
            
            // Query to get all stored procedures
            string query = @"
                SELECT ROUTINE_NAME 
                FROM INFORMATION_SCHEMA.ROUTINES 
                WHERE ROUTINE_TYPE = 'PROCEDURE' 
                ORDER BY ROUTINE_NAME";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                procedures.Add(reader.GetString(0));
            }

            return JsonSerializer.Serialize(procedures, _jsonOptions);
        }

        [McpServerTool, Description("Gets information about the database.")]
        public async Task<string> GetDatabaseInfo(CancellationToken cancellationToken)
        {
            // Test connection first
            // Use the connection service from the factory for testing and getting database info
            var connectionService = _connectionFactory.ConnectionService;
            await connectionService.TestConnectionAsync(cancellationToken);
            
            // Return database info
            var dbInfo = connectionService.GetDatabaseInfo();
            
            // Don't include connection string in the output for security
            var safeDbInfo = new
            {
                ServerName = dbInfo.ServerName,
                DatabaseName = dbInfo.DatabaseName,
                IsConnected = dbInfo.IsConnected,
                LastConnected = dbInfo.LastConnected
            };
            
            return JsonSerializer.Serialize(safeDbInfo, _jsonOptions);
        }
    }
}