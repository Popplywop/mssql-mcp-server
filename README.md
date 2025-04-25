# SQL Server MCP Server

A Model Context Protocol (MCP) server that provides tools for interacting with SQL Server databases. This server allows Large Language Models (LLMs) to query and manipulate SQL Server databases through a standardized protocol.

## Features

- **Database Querying**: Execute SQL queries and retrieve results
- **Schema Inspection**: List tables, views, stored procedures, and examine table schemas
- **Transaction Support**: Execute multiple SQL queries in a transaction

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server instance (local or remote)

### Installation

```bash
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

- **ExecuteScalarQuery**: Executes a SQL query and returns only the first value
  - Parameters:
    - `query`: The SQL query to execute
    - `commandTimeout`: Optional command timeout in seconds

- **ExecuteTransaction**: Executes multiple SQL queries in a transaction
  - Parameters:
    - `queriesJson`: List of SQL queries to execute in a transaction (JSON array)
    - `commandTimeout`: Optional command timeout in seconds

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
  query: "SELECT TOP 10 * FROM Customers WHERE Region = 'WA'"
  maxRows: 100
```

### Executing a Transaction

```
ExecuteTransaction:
  queriesJson: [
    "BEGIN TRANSACTION",
    "INSERT INTO Customers (CustomerID, CompanyName) VALUES ('DEMO1', 'Demo Company')",
    "UPDATE Orders SET ShipName = 'Demo Company' WHERE CustomerID = 'DEMO1'",
    "COMMIT"
  ]
```

## License

This project is licensed under the [MIT License](LICENSE.md) - see the [LICENSE.md](LICENSE.md) file for details.

The MIT License is a permissive license that allows for reuse with minimal restrictions. It permits anyone to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software, subject to the condition that the original copyright notice and permission notice appear in all copies.