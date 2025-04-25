using ModelContextProtocol.Server;
using SqlServerMcpServer.Services;
using System.ComponentModel;
using System.Text.Json;

namespace SqlServerMcpServer.Tools
{
    [McpServerToolType]
    public class QueryTool(QueryService queryService)
    {
        private readonly QueryService _queryService = queryService;
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        [McpServerTool, Description("Executes a SQL query against the database and returns the results.")]
        public async Task<string> ExecuteQuery(
            [Description("The SQL query to execute")] string query,
            [Description("Optional command timeout in seconds")] int? commandTimeout = null,
            [Description("Optional maximum number of rows to return")] int? maxRows = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _queryService.ExecuteQueryAsync(query, commandTimeout, maxRows, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return $"Error executing query: {result.Message}" + (result.ErrorCode.HasValue ? $" (Error code: {result.ErrorCode})" : "");
            }

            if (result.Rows.Count > 0)
            {
                // Format as JSON for structured data
                return JsonSerializer.Serialize(result, _jsonOptions);
            }
            else
            {
                return result.Message;
            }
        }

        [McpServerTool, Description("Executes a SQL query and returns only the first row as a single result.")]
        public async Task<string> ExecuteScalarQuery(
            [Description("The SQL query to execute")] string query,
            [Description("Optional command timeout in seconds")] int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            // For scalar queries, we only need one row
            var result = await _queryService.ExecuteQueryAsync(query, commandTimeout, 1, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return $"Error executing query: {result.Message}" + (result.ErrorCode.HasValue ? $" (Error code: {result.ErrorCode})" : "");
            }

            if (result.Rows.Count > 0 && result.Columns.Count > 0)
            {
                var firstRow = result.Rows[0];
                var firstColumn = result.Columns[0];
                var value = firstRow[firstColumn]?.ToString() ?? "NULL";
                return value;
            }
            else
            {
                return "No results returned";
            }
        }
        
        [McpServerTool, Description("Executes multiple SQL queries in a transaction.")]
        public async Task<string> ExecuteTransaction(
            [Description("List of SQL queries to execute in a transaction (JSON array)")] string queriesJson,
            [Description("Optional command timeout in seconds")] int? commandTimeout = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Parse the JSON array of queries
                var queries = JsonSerializer.Deserialize<List<string>>(queriesJson)
                    ?? throw new ArgumentException("Invalid JSON array of queries");
                
                if (queries.Count == 0)
                {
                    return "No queries provided for transaction";
                }
                
                var result = await _queryService.ExecuteTransactionAsync(queries, commandTimeout, cancellationToken);
                
                if (!result.IsSuccess)
                {
                    return $"Transaction failed: {result.Message}" + (result.ErrorCode.HasValue ? $" (Error code: {result.ErrorCode})" : "");
                }
                
                return result.Message;
            }
            catch (JsonException ex)
            {
                return $"Error parsing queries JSON: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error executing transaction: {ex.Message}";
            }
        }
    }
}