# SQL Server MCP Server

A Model Context Protocol (MCP) server that provides tools for interacting with SQL Server databases. This server allows Large Language Models (LLMs) to query and manipulate SQL Server databases through a standardized protocol.

## Features

- **Database Querying**: Execute SQL queries and retrieve results
- **Schema Inspection**: List tables, views, stored procedures, and examine table schemas
- **Data Manipulation**: Insert, update, delete, and truncate data

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server instance (local or remote)

### Configuration

Edit the `appsettings.json` file to configure your SQL Server connection:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"
  }
}
```

### Running the Server

```bash
dotnet run
```

## Available MCP Tools

### Query Tools

- **ExecuteQuery**: Executes a SQL query and returns the results
- **ExecuteScalarQuery**: Executes a SQL query and returns only the first value

### Schema Tools

- **ListTables**: Lists all tables in the database
- **GetTableSchema**: Gets the schema of a specific table
- **ListViews**: Lists all views in the database
- **ListStoredProcedures**: Lists all stored procedures in the database
- **GetDatabaseInfo**: Gets information about the connected database

### Data Manipulation Tools

- **InsertRecord**: Inserts a new record into a table
- **UpdateRecords**: Updates records in a table based on a condition
- **DeleteRecords**: Deletes records from a table based on a condition
- **TruncateTable**: Truncates a table (removes all records)

## Security Considerations

- Use a SQL Server account with appropriate permissions (principle of least privilege)
- Store connection strings securely (not in source control)
- Consider using Azure Key Vault or similar services for production deployments
- Enable TLS/SSL for database connections

## Example Usage

### Querying Data

```
ExecuteQuery: "SELECT TOP 10 * FROM Customers WHERE Region = 'WA'"
```

### Inserting Data

```
InsertRecord: 
  tableName: "Customers"
  recordJson: {
    "CustomerID": "DEMO1",
    "CompanyName": "Demo Company",
    "ContactName": "John Doe",
    "Country": "USA"
  }
```

## License

MIT