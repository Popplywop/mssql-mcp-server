using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SqlServerMcpServer.Models;

namespace SqlServerMcpServer.Services
{
    public class QueryService
    {
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<QueryService> _logger;
        private readonly SqlInjectionValidationService _sqlInjectionValidator;
        private readonly int _defaultCommandTimeout;
        private readonly int _defaultMaxRows;

        public QueryService(
            SqlConnectionFactory connectionFactory,
            ILogger<QueryService> logger,
            SqlInjectionValidationService sqlInjectionValidator,
            IConfiguration configuration)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqlInjectionValidator = sqlInjectionValidator ?? throw new ArgumentNullException(nameof(sqlInjectionValidator));
            
            // Get configuration values with defaults
            _defaultCommandTimeout = configuration.GetValue<int>("Database:DefaultCommandTimeout", 30);
            _defaultMaxRows = configuration.GetValue<int>("Database:DefaultMaxRows", 1000);
        }

        /// <summary>
        /// Executes a SQL query with pagination and configurable timeout
        /// </summary>
        /// <param name="query">The SQL query to execute</param>
        /// <param name="commandTimeout">Optional command timeout in seconds</param>
        /// <param name="maxRows">Optional maximum number of rows to return</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query result</returns>
        public async Task<QueryResult> ExecuteQueryAsync(
            string query,
            int? commandTimeout = null,
            int? maxRows = null,
            CancellationToken cancellationToken = default)
        {
            // Use default values if not specified
            int timeout = commandTimeout ?? _defaultCommandTimeout;
            int rowLimit = maxRows ?? _defaultMaxRows;
            
            _logger.LogDebug("Executing query with timeout {Timeout}s and max rows {MaxRows}", timeout, rowLimit);
            
            // Validate the query for SQL injection
            var (isValid, errorMessage) = _sqlInjectionValidator.ValidateQuery(query);
            if (!isValid)
            {
                _logger.LogWarning("SQL injection validation failed: {ErrorMessage}", errorMessage);
                return new QueryResult
                {
                    IsSuccess = false,
                    Message = $"Security validation failed: {errorMessage}"
                };
            }
            
            using var connection = _connectionFactory.CreateConnection(timeout);
            
            try
            {
                await connection.OpenAsync(cancellationToken);
                
                using var command = new SqlCommand(query, connection);
                command.CommandTimeout = timeout;

                // For SELECT queries
                if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    return await ReadQueryResultAsync(reader, rowLimit, cancellationToken);
                }
                // For INSERT, UPDATE, DELETE queries
                else
                {
                    int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                    return new QueryResult
                    {
                        RowsAffected = rowsAffected,
                        IsSuccess = true,
                        Message = $"{rowsAffected} row(s) affected."
                    };
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error executing query: {Message}", ex.Message);
                return new QueryResult
                {
                    IsSuccess = false,
                    Message = $"SQL Error: {ex.Message}",
                    ErrorCode = ex.Number
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing query: {Message}", ex.Message);
                return new QueryResult
                {
                    IsSuccess = false,
                    Message = ex.Message
                };
            }
        }
        
        private async Task<QueryResult> ReadQueryResultAsync(
            SqlDataReader reader,
            int maxRows,
            CancellationToken cancellationToken)
        {
            var result = new QueryResult
            {
                Columns = [],
                Rows = [],
                IsSuccess = true
            };

            try
            {
                // Get column names
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result.Columns.Add(reader.GetName(i));
                }

                // Read rows with pagination
                int rowCount = 0;
                bool hasMoreRows = false;
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (rowCount >= maxRows)
                    {
                        hasMoreRows = true;
                        break;
                    }
                    
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        // Use DBNull.Value instead of null to avoid nullable reference warning
                        var value = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                        row[columnName] = value;
                    }
                    result.Rows.Add(row);
                    rowCount++;
                }

                result.RowCount = result.Rows.Count;
                result.HasMoreRows = hasMoreRows;
                
                if (hasMoreRows)
                {
                    result.Message = $"Query returned {result.RowCount} rows (limited from a larger result set). Use pagination parameters to see more results.";
                    _logger.LogInformation("Query result was limited to {MaxRows} rows", maxRows);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading query results: {Message}", ex.Message);
                throw;
            }
        }
    }
}