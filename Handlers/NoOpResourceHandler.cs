using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace SqlServerMcpServer.Handlers
{
    /// <summary>
    /// Handles resource requests for the SQL Server MCP Server.
    /// Currently implements a "no operation" approach that returns structured empty results.
    /// </summary>
    public class NoOpResourceHandler(ILogger<NoOpResourceHandler> logger)
    {
        private readonly ILogger<NoOpResourceHandler>? _logger = logger;

        /// <summary>
        /// Handles requests to list available resources.
        /// </summary>
        /// <param name="context">The request context containing parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of available resources (currently empty)</returns>
        public ValueTask<ListResourcesResult> HandleListResources(
            RequestContext<ListResourcesRequestParams> context,
            CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Resource listing requested");
            
            // Return an empty list of resources
            return new ValueTask<ListResourcesResult>(
                new ListResourcesResult
                {
                    Resources = new List<Resource>()
                });
        }

        /// <summary>
        /// Handles requests to read a specific resource.
        /// </summary>
        /// <param name="context">The request context containing parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The resource content (currently empty)</returns>
        public ValueTask<ReadResourceResult> HandleReadResources(
            RequestContext<ReadResourceRequestParams> context,
            CancellationToken cancellationToken)
        {
            string resourceUri = context.Params?.Uri ?? "unknown";
            _logger?.LogInformation("Resource read requested for URI: {Uri}", resourceUri);
            
            // Return an empty result
            return new ValueTask<ReadResourceResult>(
                new ReadResourceResult
                {
                    Contents = new List<ResourceContents>()
                });
        }
    }
}
