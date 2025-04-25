using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlServerMcpServer.Services;
using SqlServerMcpServer.Handlers;
using System.CommandLine;

namespace SqlServerMcpServer
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Create the root command
            var rootCommand = new RootCommand("SQL Server MCP Server - Provides SQL Server database access via Model Context Protocol");

            // Add options
            var dsnOption = new Option<string>(
                name: "--dsn",
                description: "SQL Server connection string (e.g., \"Server=myserver;Database=mydb;User Id=sa;Password=mypassword;TrustServerCertificate=True;\")");
            dsnOption.AddAlias("-d");
            
            var envVarOption = new Option<string>(
                name: "--env-var",
                description: "Environment variable name containing the connection string");
            envVarOption.AddAlias("-e");
            
            // Add options to the root command
            rootCommand.AddOption(dsnOption);
            rootCommand.AddOption(envVarOption);

            // Set the handler for the root command
            rootCommand.SetHandler(async (dsn, envVar) =>
            {
                try
                {
                    // Determine the connection string from the provided options
                    string? connectionString = null;
                    
                    // Check environment variable first if specified
                    if (!string.IsNullOrEmpty(envVar))
                    {
                        connectionString = Environment.GetEnvironmentVariable(envVar);
                        if (string.IsNullOrEmpty(connectionString))
                        {
                            Console.Error.WriteLine($"Error: Environment variable '{envVar}' not found or empty");
                            Environment.Exit(1);
                        }
                    }
                    // Otherwise use the DSN option
                    else if (!string.IsNullOrEmpty(dsn))
                    {
                        connectionString = dsn;
                    }
                    
                    // If no connection string was provided, show error and exit
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Console.Error.WriteLine("Error: No connection string provided. Use --dsn or --env-var option.");
                        Environment.Exit(1);
                    }
                    
                    // Create the host builder
                    var builder = Host.CreateApplicationBuilder(args);
                    
                    // Add the connection string to configuration
                    builder.Configuration["ConnectionStrings:SqlServer"] = connectionString;
                    
                    // Configure logging
                    var logLevel = LogLevel.Information;
                    builder.Logging.AddConsole(consoleLogOptions =>
                    {
                        // Configure all logs to go to stderr
                        consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
                    });
                    builder.Logging.SetMinimumLevel(logLevel);
                    
                    // Register services
                    // Configure default database settings
                    builder.Configuration["Database:DefaultCommandTimeout"] = "30";
                    builder.Configuration["Database:DefaultMaxRows"] = "1000";
                    
                    // Register services in the correct order of dependency
                    builder.Services
                        // Register SqlConnectionService first
                        .AddSingleton<SqlConnectionService>()
                        // Register SqlConnectionFactory which depends on SqlConnectionService
                        .AddSingleton<SqlConnectionFactory>()
                        // Register SQL injection validation service
                        .AddSingleton<SqlInjectionValidationService>()
                        // Register services that depend on SqlConnectionFactory
                        .AddSingleton<QueryService>()
                        .AddSingleton<NoOpResourceHandler>();
                    
                    // Get the handler instance from the service provider
                    var serviceProvider = builder.Services.BuildServiceProvider();
                    var resourceHandler = serviceProvider.GetRequiredService<NoOpResourceHandler>();
                    
                    // Configure MCP server
                    builder.Services
                        .AddMcpServer()
                        .WithStdioServerTransport()
                        .WithToolsFromAssembly()
                        .WithListResourcesHandler(resourceHandler.HandleListResources)
                        .WithReadResourceHandler(resourceHandler.HandleReadResources);
                    
                    // Build and run the host
                    var host = builder.Build();
                    
                    // Log startup information
                    var logger = host.Services.GetRequiredService<ILogger<Program>>();
                    var connectionFactory = host.Services.GetRequiredService<SqlConnectionFactory>();
                    var connectionService = connectionFactory.ConnectionService;
                    
                    logger.LogInformation("Starting SQL Server MCP Server...");
                    
                    try
                    {
                        // Test database connection
                        await connectionService.TestConnectionAsync();
                        
                        // If we get here, connection was successful
                        var dbInfo = connectionService.GetDatabaseInfo();
                        logger.LogInformation("Connected to database {Database} on server {Server}",
                            dbInfo.DatabaseName, dbInfo.ServerName);
                        
                        // Run the application
                        await host.RunAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to start SQL Server MCP Server");
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Unhandled error: {ex.Message}");
                    Environment.Exit(1);
                }
            }, dsnOption, envVarOption);
            
            // Parse the command line arguments and execute the handler
            return await rootCommand.InvokeAsync(args);
        }
    }
}
