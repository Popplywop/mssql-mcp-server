using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using Services;
using System.Text.Json;

namespace Handlers
{
    /// <summary>
    /// Handles database resource requests for the SQL Server MCP Server.
    /// Provides access to database schema information as resources.
    /// </summary>
    public class DatabaseResourceHandler
    {
        private readonly ILogger<DatabaseResourceHandler> _logger;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly DatabaseMetadataCache _metadataCache;
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        // Define resource URI prefixes
        private const string RESOURCE_PREFIX = "sqlserver://";
        
        // Schema-specific resource URI prefixes
        private const string SCHEMA_PREFIX = RESOURCE_PREFIX + "schemas/";
        private const string SCHEMA_TABLES_SUFFIX = "/tables";
        private const string SCHEMA_VIEWS_SUFFIX = "/views";
        private const string SCHEMA_PROCEDURES_SUFFIX = "/procedures";

        public DatabaseResourceHandler(
            ILogger<DatabaseResourceHandler> logger,
            SqlConnectionFactory connectionFactory,
            DatabaseMetadataCache metadataCache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _metadataCache = metadataCache ?? throw new ArgumentNullException(nameof(metadataCache));
        }

        /// <summary>
        /// Handles requests to list available database resources.
        /// </summary>
        /// <param name="context">The request context containing parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of available database resources</returns>
        public async ValueTask<ListResourcesResult> HandleListResources(
            RequestContext<ListResourcesRequestParams> context,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Database resource listing requested");

            var resources = new List<Resource>();

            // Add schema-specific resources
            try
            {
                // Only fetch schemas initially - this is the only database query we'll make upfront
                var schemas = await _metadataCache.GetOrAddAsync(
                    "schemas",
                    () => GetSchemasAsync(cancellationToken));
                
                foreach (var schema in schemas)
                {
                    // Add schema container resource
                    resources.Add(new Resource
                    {
                        Uri = $"{SCHEMA_PREFIX}{schema}",
                        Name = $"Schema: {schema}",
                        Description = $"Resources for the {schema} schema",
                        MimeType = "application/json"
                    });
                    
                    // Add tables container resource (without fetching actual tables)
                    resources.Add(new Resource
                    {
                        Uri = $"{SCHEMA_PREFIX}{schema}{SCHEMA_TABLES_SUFFIX}",
                        Name = $"Tables in {schema}",
                        Description = $"List of tables in the {schema} schema",
                        MimeType = "application/json"
                    });
                    
                    // Add views container resource (without fetching actual views)
                    resources.Add(new Resource
                    {
                        Uri = $"{SCHEMA_PREFIX}{schema}{SCHEMA_VIEWS_SUFFIX}",
                        Name = $"Views in {schema}",
                        Description = $"List of views in the {schema} schema",
                        MimeType = "application/json"
                    });
                    
                    // Add procedures container resource (without fetching actual procedures)
                    resources.Add(new Resource
                    {
                        Uri = $"{SCHEMA_PREFIX}{schema}{SCHEMA_PROCEDURES_SUFFIX}",
                        Name = $"Procedures in {schema}",
                        Description = $"List of stored procedures in the {schema} schema",
                        MimeType = "application/json"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schema resources for resource listing");
            }

            return new ListResourcesResult
            {
                Resources = resources
            };
        }

        /// <summary>
        /// Handles requests to read a specific database resource.
        /// </summary>
        /// <param name="context">The request context containing parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The resource content</returns>
        public async ValueTask<ReadResourceResult> HandleReadResources(
            RequestContext<ReadResourceRequestParams> context,
            CancellationToken cancellationToken)
        {
            string resourceUri = context.Params?.Uri ?? "unknown";
            _logger.LogInformation("Database resource read requested for URI: {Uri}", resourceUri);
            try
            {
                var contents = await GetResourceContentsAsync(resourceUri, cancellationToken);
                return new ReadResourceResult
                {
                    Contents = contents
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving resource: {Uri}", resourceUri);
                return new ReadResourceResult
                {
                    Contents =
                    [
                        new TextResourceContents
                        {
                            MimeType = "text/plain",
                            Text = $"Error retrieving resource: {ex.Message}"
                        }
                    ]
                };
            }
        }

        private async Task<List<ResourceContents>> GetResourceContentsAsync(string uri, CancellationToken cancellationToken)
        {
            string jsonContent;

            if (uri.StartsWith(SCHEMA_PREFIX))
            {
                string remaining = uri[SCHEMA_PREFIX.Length..];
                
                // Handle schema/tables pattern
                if (remaining.Contains(SCHEMA_TABLES_SUFFIX))
                {
                    string schemaAndTables = remaining;
                    string schemaName = schemaAndTables[..schemaAndTables.IndexOf(SCHEMA_TABLES_SUFFIX)];
                    string afterTables = schemaAndTables[(schemaAndTables.IndexOf(SCHEMA_TABLES_SUFFIX) + SCHEMA_TABLES_SUFFIX.Length)..];
                    
                    if (string.IsNullOrEmpty(afterTables))
                    {
                        // List all tables in schema: sqlserver://schema/dbo/tables
                        // This is where we lazily load the tables for this schema
                        var tables = await _metadataCache.GetOrAddAsync(
                            $"tables_{schemaName}",
                            () => GetTablesBySchemaAsync(schemaName, cancellationToken));
                        jsonContent = JsonSerializer.Serialize(tables, _jsonOptions);
                    }
                    else
                    {
                        // Get specific table: sqlserver://schema/dbo/tables/tablename
                        string tableName = afterTables.TrimStart('/');
                        var tableInfo = await _metadataCache.GetOrAddAsync(
                            $"table_{schemaName}_{tableName}",
                            () => GetTableInfoAsync(schemaName, tableName, cancellationToken));
                        jsonContent = JsonSerializer.Serialize(tableInfo, _jsonOptions);
                    }
                }
                // Handle schema/views pattern
                else if (remaining.Contains(SCHEMA_VIEWS_SUFFIX))
                {
                    string schemaAndViews = remaining;
                    string schemaName = schemaAndViews[..schemaAndViews.IndexOf(SCHEMA_VIEWS_SUFFIX)];
                    string afterViews = schemaAndViews[(schemaAndViews.IndexOf(SCHEMA_VIEWS_SUFFIX) + SCHEMA_VIEWS_SUFFIX.Length)..];
                    
                    if (string.IsNullOrEmpty(afterViews))
                    {
                        // List all views in schema: sqlserver://schema/dbo/views
                        var views = await _metadataCache.GetOrAddAsync(
                            $"views_{schemaName}",
                            () => GetViewsBySchemaAsync(schemaName, cancellationToken));
                        jsonContent = JsonSerializer.Serialize(views, _jsonOptions);
                    }
                    else
                    {
                        // Get specific view: sqlserver://schema/dbo/views/viewname
                        string viewName = afterViews.TrimStart('/');
                        var viewInfo = await _metadataCache.GetOrAddAsync(
                            $"view_{schemaName}_{viewName}",
                            () => GetViewInfoAsync(schemaName, viewName, cancellationToken));
                        jsonContent = JsonSerializer.Serialize(viewInfo, _jsonOptions);
                    }
                }
                // Handle schema/procedures pattern
                else if (remaining.Contains(SCHEMA_PROCEDURES_SUFFIX))
                {
                    string schemaAndProcedures = remaining;
                    string schemaName = schemaAndProcedures[..schemaAndProcedures.IndexOf(SCHEMA_PROCEDURES_SUFFIX)];
                    string afterProcedures = schemaAndProcedures[(schemaAndProcedures.IndexOf(SCHEMA_PROCEDURES_SUFFIX) + SCHEMA_PROCEDURES_SUFFIX.Length)..];
                    
                    if (string.IsNullOrEmpty(afterProcedures))
                    {
                        // List all procedures in schema: sqlserver://schema/dbo/procedures
                        var procedures = await _metadataCache.GetOrAddAsync(
                            $"procedures_{schemaName}",
                            () => GetStoredProceduresBySchemaAsync(schemaName, cancellationToken));
                        jsonContent = JsonSerializer.Serialize(procedures, _jsonOptions);
                    }
                    else
                    {
                        // Get specific procedure: sqlserver://schema/dbo/procedures/procedurename
                        string procedureName = afterProcedures.TrimStart('/');
                        var procedureInfo = await _metadataCache.GetOrAddAsync(
                            $"procedure_{schemaName}_{procedureName}",
                            () => GetProcedureInfoAsync(schemaName, procedureName, cancellationToken));
                        jsonContent = JsonSerializer.Serialize(procedureInfo, _jsonOptions);
                    }
                }
                else
                {
                    // Just the schema name, return general schema info
                    string schemaName = remaining;
                    var schemaInfo = await _metadataCache.GetOrAddAsync(
                        $"schema_{schemaName}",
                        () => GetSchemaInfoAsync(schemaName, cancellationToken));
                    jsonContent = JsonSerializer.Serialize(schemaInfo, _jsonOptions);
                }
            }
            else
            {
                throw new ArgumentException($"Unknown resource URI: {uri}");
            }

            return
            [
                new TextResourceContents
                {
                    MimeType = "application/json",
                    Text = jsonContent
                }
            ];
        }

        private async Task<List<string>> GetTablesBySchemaAsync(string schemaName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60); // Longer timeout for schema operations
            await connection.OpenAsync(cancellationToken);

            var tables = new List<string>();

            // Query to get all user tables for a specific schema
            string query = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                AND TABLE_SCHEMA = @SchemaName
                ORDER BY TABLE_NAME";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }

        private async Task<List<string>> GetViewsBySchemaAsync(string schemaName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            var views = new List<string>();

            // Query to get all views for a specific schema
            string query = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.VIEWS
                WHERE TABLE_SCHEMA = @SchemaName
                ORDER BY TABLE_NAME";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                views.Add(reader.GetString(0));
            }

            return views;
        }

        private async Task<List<string>> GetStoredProceduresBySchemaAsync(string schemaName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            var procedures = new List<string>();

            // Query to get all stored procedures for a specific schema
            string query = @"
                SELECT ROUTINE_NAME
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_TYPE = 'PROCEDURE'
                AND ROUTINE_SCHEMA = @SchemaName
                ORDER BY ROUTINE_NAME";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SchemaName", schemaName);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                procedures.Add(reader.GetString(0));
            }

            return procedures;
        }

        private async Task<List<string>> GetSchemasAsync(CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60); // Longer timeout for schema operations
            await connection.OpenAsync(cancellationToken);

            var schemas = new List<string>();

            // Query to get all schemas where the owner is dbo
            string query = @"
                SELECT DISTINCT SCHEMA_NAME
                FROM INFORMATION_SCHEMA.SCHEMATA
                WHERE SCHEMA_OWNER = @Owner
                ORDER BY SCHEMA_NAME";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Owner", "dbo");
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                schemas.Add(reader.GetString(0));
            }

            return schemas;
        }
        private async Task<object> GetSchemaInfoAsync(string schemaName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            // Get counts of objects in this schema - use cache to avoid redundant queries
            var tableCount = (await _metadataCache.GetOrAddAsync(
                $"tables_{schemaName}",
                () => GetTablesBySchemaAsync(schemaName, cancellationToken))).Count;
                
            var viewCount = (await _metadataCache.GetOrAddAsync(
                $"views_{schemaName}",
                () => GetViewsBySchemaAsync(schemaName, cancellationToken))).Count;
                
            var procedureCount = (await _metadataCache.GetOrAddAsync(
                $"procedures_{schemaName}",
                () => GetStoredProceduresBySchemaAsync(schemaName, cancellationToken))).Count;

            // Return schema info
            return new
            {
                SchemaName = schemaName,
                Tables = tableCount,
                Views = viewCount,
                StoredProcedures = procedureCount
            };
        }

        private async Task<object> GetTableInfoAsync(string schemaName, string tableName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            // Get table columns
            var columns = new List<object>();
            string columnsQuery = @"
                SELECT
                    COLUMN_NAME,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    IS_NULLABLE,
                    COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @SchemaName
                AND TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            using (var command = new SqlCommand(columnsQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@TableName", tableName);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    columns.Add(new
                    {
                        Name = reader.GetString(0),
                        DataType = reader.GetString(1),
                        MaxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                        IsNullable = reader.GetString(3),
                        DefaultValue = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                    });
                }
            }

            // Get primary key information
            var primaryKeys = new List<string>();
            string pkQuery = @"
                SELECT COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + CONSTRAINT_NAME), 'IsPrimaryKey') = 1
                AND TABLE_SCHEMA = @SchemaName
                AND TABLE_NAME = @TableName
                ORDER BY ORDINAL_POSITION";

            using (var command = new SqlCommand(pkQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@TableName", tableName);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    primaryKeys.Add(reader.GetString(0));
                }
            }

            // Get foreign key information
            var foreignKeys = new List<object>();
            string fkQuery = @"
                SELECT
                    fk.name AS FK_NAME,
                    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS COLUMN_NAME,
                    OBJECT_SCHEMA_NAME(fc.referenced_object_id) AS REFERENCED_SCHEMA,
                    OBJECT_NAME(fc.referenced_object_id) AS REFERENCED_TABLE,
                    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS REFERENCED_COLUMN
                FROM
                    sys.foreign_keys AS fk
                INNER JOIN
                    sys.foreign_key_columns AS fc ON fk.OBJECT_ID = fc.constraint_object_id
                WHERE
                    OBJECT_SCHEMA_NAME(fk.parent_object_id) = @SchemaName
                    AND OBJECT_NAME(fk.parent_object_id) = @TableName
                ORDER BY
                    fk.name, fc.constraint_column_id";

            using (var command = new SqlCommand(fkQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@TableName", tableName);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    foreignKeys.Add(new
                    {
                        Name = reader.GetString(0),
                        ColumnName = reader.GetString(1),
                        ReferencedSchema = reader.GetString(2),
                        ReferencedTable = reader.GetString(3),
                        ReferencedColumn = reader.GetString(4)
                    });
                }
            }

            // Return table info
            return new
            {
                Schema = schemaName,
                TableName = tableName,
                Columns = columns,
                PrimaryKeys = primaryKeys,
                ForeignKeys = foreignKeys
            };
        }

        private async Task<object> GetViewInfoAsync(string schemaName, string viewName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            // Get view columns
            var columns = new List<object>();
            string columnsQuery = @"
                SELECT
                    COLUMN_NAME,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    IS_NULLABLE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @SchemaName
                AND TABLE_NAME = @ViewName
                ORDER BY ORDINAL_POSITION";

            using (var command = new SqlCommand(columnsQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ViewName", viewName);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    columns.Add(new
                    {
                        Name = reader.GetString(0),
                        DataType = reader.GetString(1),
                        MaxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                        IsNullable = reader.GetString(3)
                    });
                }
            }

            // Get view definition
            string definition = string.Empty;
            string definitionQuery = @"
                SELECT VIEW_DEFINITION
                FROM INFORMATION_SCHEMA.VIEWS
                WHERE TABLE_SCHEMA = @SchemaName
                AND TABLE_NAME = @ViewName";

            using (var command = new SqlCommand(definitionQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ViewName", viewName);
                var result = await command.ExecuteScalarAsync(cancellationToken);
                if (result != null && result != DBNull.Value)
                {
                    definition = (string)result;
                }
            }

            // Return view info
            return new
            {
                Schema = schemaName,
                ViewName = viewName,
                Columns = columns,
                Definition = definition
            };
        }

        private async Task<object> GetProcedureInfoAsync(string schemaName, string procedureName, CancellationToken cancellationToken)
        {
            using var connection = _connectionFactory.CreateConnection(60);
            await connection.OpenAsync(cancellationToken);

            // Get procedure parameters
            var parameters = new List<object>();
            string paramsQuery = @"
                SELECT
                    PARAMETER_NAME,
                    PARAMETER_MODE,
                    DATA_TYPE,
                    CHARACTER_MAXIMUM_LENGTH,
                    PARAMETER_DEFAULT
                FROM INFORMATION_SCHEMA.PARAMETERS
                WHERE SPECIFIC_SCHEMA = @SchemaName
                AND SPECIFIC_NAME = @ProcedureName
                ORDER BY ORDINAL_POSITION";

            using (var command = new SqlCommand(paramsQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ProcedureName", procedureName);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    parameters.Add(new
                    {
                        Name = reader.IsDBNull(0) ? null : reader.GetString(0),
                        Mode = reader.GetString(1),
                        DataType = reader.GetString(2),
                        MaxLength = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                        DefaultValue = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                    });
                }
            }

            // Get procedure definition
            string definition = string.Empty;
            string definitionQuery = @"
                SELECT ROUTINE_DEFINITION
                FROM INFORMATION_SCHEMA.ROUTINES
                WHERE ROUTINE_SCHEMA = @SchemaName
                AND ROUTINE_NAME = @ProcedureName
                AND ROUTINE_TYPE = 'PROCEDURE'";

            using (var command = new SqlCommand(definitionQuery, connection))
            {
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ProcedureName", procedureName);
                var result = await command.ExecuteScalarAsync(cancellationToken);
                if (result != null && result != DBNull.Value)
                {
                    definition = (string)result;
                }
            }

            // Return procedure info
            return new
            {
                Schema = schemaName,
                ProcedureName = procedureName,
                Parameters = parameters,
                Definition = definition
            };
        }
    }
}
