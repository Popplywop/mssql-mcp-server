# 🚧 WORK IN PROGRESS 🚧

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

#### Option 1: Install as a .NET Tool (Recommended)

Install globally as a .NET tool:

```bash
dotnet tool install --global tsql-mcp-server
```

To update to the latest version:

```bash
dotnet tool update --global tsql-mcp-server
```

#### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/Popplywop/mssql-mcp-server
cd mssql-mcp-server

# Build the project
dotnet build
```

### Running the Server

#### Using the .NET Tool

If installed as a .NET global tool:

```bash
# Using a direct connection string
tsql-mcp-server --dsn "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"

# Using an environment variable
tsql-mcp-server --env-var "SQL_CONNECTION_STRING"
```

#### Using Source Code

If building from source:

```bash
# Using a direct connection string
dotnet run --dsn "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"

# Using an environment variable
dotnet run --env-var "SQL_CONNECTION_STRING"
```

### MCP Server Configuration

To use this server with Claude or other LLMs that support the Model Context Protocol, you'll need to configure it in your MCP configuration file.

#### If Installed as a .NET Tool

```json
{
  "servers": [
    {
      "name": "SqlServerMcp",
      "command": "tsql-mcp-server",
      "args": [
        "--dsn",
        "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"
      ]
    }
  ]
}
```

#### If Built from Source

```json
{
  "servers": [
    {
      "name": "SqlServerMcp",
      "command": "path/to/mssql-mcp-server.exe",
      "args": [
        "--dsn",
        "Server=your-server;Database=your-database;User Id=your-username;Password=your-password;TrustServerCertificate=True;"
      ]
    }
  ]
}
```

Replace the connection string with your actual values. This configuration can be used with Claude's MCP integration or other LLM platforms that support the Model Context Protocol.

#### Using Environment Variables for Sensitive Information

For better security, you can use environment variables for your connection string:

```json
{
  "servers": [
    {
      "name": "SqlServerMcp",
      "command": "tsql-mcp-server",
      "args": [
        "--env-var",
        "SQL_CONNECTION_STRING"
      ]
    }
  ]
}
```

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