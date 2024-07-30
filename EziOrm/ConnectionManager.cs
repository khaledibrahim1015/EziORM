using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace EziOrm
{
    /// <summary>
    ///    This implementation uses a lazy initialization pattern to create the connection only when needed.
    //     It ensures thread-safety with a lock mechanism.
    //     The GetConnectionAsync method returns an open connection, opening it if necessary.
    //     The class implements IDisposable to properly manage the connection lifecycle
    /// </summary>
    public class ConnectionManager : IDisposable
    {

        private readonly string _connectionString;
        private DbConnection _connection;
        private readonly object _lock = new object();
        public ConnectionManager(string connectionString)
            => _connectionString = connectionString;

        public async Task<DbConnection> GetConnectionAsync()
        {
            if (_connection == null)
                lock (_lock)
                    if (_connection == null)
                        _connection = new SqlConnection(_connectionString);

            if (_connection.State != ConnectionState.Open)
                _connection.OpenAsync();

            return _connection;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
