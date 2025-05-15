using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Services
{
    /// <summary>
    /// Factory for creating and managing SQL connections with proper tracking and disposal
    /// </summary>
    public class SqlConnectionFactory : IDisposable
    {
        private readonly SqlConnectionService _connectionService;
        private readonly ILogger<SqlConnectionFactory> _logger;
        private readonly ConcurrentDictionary<Guid, SqlConnection> _activeConnections = new();
        private bool _disposed = false;

        public SqlConnectionFactory(SqlConnectionService connectionService, ILogger<SqlConnectionFactory> logger)
        {
            _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a new SQL connection with tracking
        /// </summary>
        /// <returns>A tracked SQL connection</returns>
        public SqlConnection CreateConnection(int? commandTimeout = null)
        {
            ThrowIfDisposed();
            
            var connection = _connectionService.CreateConnection(commandTimeout);
            var connectionId = Guid.NewGuid();
            
            // Track the connection
            _activeConnections.TryAdd(connectionId, connection);
            _logger.LogDebug("Created new SQL connection. Active connections: {Count}", _activeConnections.Count);
            
            // Wrap the connection in a tracking proxy that will remove it from tracking when disposed
            return new TrackedSqlConnection(connection, () => 
            {
                if (_activeConnections.TryRemove(connectionId, out _))
                {
                    _logger.LogDebug("Removed SQL connection from tracking. Active connections: {Count}", _activeConnections.Count);
                }
            });
        }

        /// <summary>
        /// Gets the count of active connections
        /// </summary>
        public int ActiveConnectionCount => _activeConnections.Count;
        
        /// <summary>
        /// Gets the underlying SqlConnectionService
        /// </summary>
        public SqlConnectionService ConnectionService => _connectionService;

        /// <summary>
        /// Disposes all tracked connections
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Close and dispose all tracked connections
                foreach (var connection in _activeConnections.Values)
                {
                    try
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing SQL connection");
                    }
                }
                
                _activeConnections.Clear();
                _logger.LogInformation("Disposed all tracked SQL connections");
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SqlConnectionFactory));
            }
        }

        /// <summary>
        /// A wrapper around SqlConnection that notifies when disposed
        /// </summary>
        private class TrackedSqlConnection : IDisposable
        {
            private readonly SqlConnection _innerConnection;
            private readonly Action _onDispose;
            private bool _disposed = false;

            public TrackedSqlConnection(SqlConnection innerConnection, Action onDispose)
            {
                _innerConnection = innerConnection ?? throw new ArgumentNullException(nameof(innerConnection));
                _onDispose = onDispose;
            }

            // Delegate all SqlConnection members to the inner connection
            public string ConnectionString
            {
                get => _innerConnection.ConnectionString;
                set => _innerConnection.ConnectionString = value;
            }

            public string Database => _innerConnection.Database;
            public string DataSource => _innerConnection.DataSource;
            public string ServerVersion => _innerConnection.ServerVersion;
            public int ConnectionTimeout => _innerConnection.ConnectionTimeout;
            public bool StatisticsEnabled
            {
                get => _innerConnection.StatisticsEnabled;
                set => _innerConnection.StatisticsEnabled = value;
            }

            // Delegate methods
            public Task OpenAsync(CancellationToken cancellationToken = default) =>
                _innerConnection.OpenAsync(cancellationToken);
            
            public void Open() => _innerConnection.Open();
            
            public void Close() => _innerConnection.Close();
            
            public SqlCommand CreateCommand() => _innerConnection.CreateCommand();
            
            public SqlTransaction BeginTransaction() => _innerConnection.BeginTransaction();
            
            public SqlTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel) =>
                _innerConnection.BeginTransaction(isolationLevel);

            // Implement IDisposable
            public void Dispose()
            {
                if (_disposed)
                    return;

                _innerConnection.Dispose();
                _onDispose?.Invoke();
                
                _disposed = true;
                GC.SuppressFinalize(this);
            }

            // Implicit conversion to SqlConnection for compatibility
            public static implicit operator SqlConnection(TrackedSqlConnection tracked) =>
                tracked._innerConnection;
        }
    }
}