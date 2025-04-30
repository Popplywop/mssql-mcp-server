# SQL Server MCP Server

A Model Context Protocol (MCP) server that provides tools for interacting with SQL Server databases. This server allows Large Language Models (LLMs) to query and inspect SQL Server databases through a standardized protocol.

## Features

- **Database Querying**: Execute SQL queries and retrieve results
- **Schema Inspection**: List tables, views, stored procedures, and examine table schemas

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server instance (local or remote)

### Installation

```bash
# Clone the repository
git clone https://github.com/Popplywop/mssql-mcp-server 
cd mssql-mcp-server

# Build the project
dotnet build
```

### Running the Server

The server can be run with a direct connection string or by referencing an environment variable:

```bash
# Using a direct connection string
dotnet run --dsn "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"

# Using an environment variable
dotnet run --env-var "SQL_CONNECTION_STRING"
```

### MCP Server Configuration

To use this server with Claude or other LLMs that support the Model Context Protocol, you'll need to configure it in your MCP configuration. Here's an example JSON configuration:

```json
{
  "servers": [
    {
      "name": "SqlServerMcp",
      "command": "path/to/mssql-mcp-server.exe",
      "args": [
        "--dsn",
        "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"
      ],
    }
  ]
}
```

Replace the path and connection string with your actual values. This configuration can be used with Claude's MCP integration or other LLM platforms that support the Model Context Protocol.

## Command Line Options

- `--dsn` or `-d`: SQL Server connection string
- `--env-var` or `-e`: Environment variable name containing the connection string

## Available MCP Tools

### Query Tools

- **ExecuteQuery**: Executes a SQL query against the database and returns the results
  - Parameters:
    - `query`: The SQL query to execute
    - `commandTimeout`: Optional command timeout in seconds
    - `maxRows`: Optional maximum number of rows to return

### Schema Tools

- **ListTables**: Lists all tables in the database

- **GetTableSchema**: Gets the schema of a specific table
  - Parameters:
    - `tableName`: The name of the table

- **ListViews**: Lists all views in the database

- **ListStoredProcedures**: Lists all stored procedures in the database

- **GetDatabaseInfo**: Gets information about the connected database

## Security Considerations

- Use a SQL Server account with appropriate permissions (principle of least privilege)
- Store connection strings securely (not in source control)
- Consider using environment variables for connection strings
- Enable TLS/SSL for database connections

## Example Usage

### Querying Data

```
ExecuteQuery:
  query: "SELECT TOP 10 * FROM MyTable"
  maxRows: 100
```

## License

This project is licensed under the [MIT License](LICENSE.md) - see the [LICENSE.md](LICENSE.md) file for details.

The MIT License is a permissive license that allows for reuse with minimal restrictions. It permits anyone to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software, subject to the condition that the original copyright notice and permission notice appear in all copies.