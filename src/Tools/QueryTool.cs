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
    }
}