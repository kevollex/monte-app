using Microsoft.Data.SqlClient;

namespace MonteApp.ApiService.Infrastructure
{
    public interface IDatabase
    {
        Task<string> GetSQLServerInfoAsync();
     Task<int> InsertNotificationAsync(string title, string body);
        Task InsertNotificationQueueAsync(int notificationId, int deviceId);
    }

    public class Database : IDatabase
    {
        private readonly SqlConnection _connection;

        // Constructor inyectado por DI
        public Database(SqlConnection connection)
        {
            _connection = connection;
        }

        // Propiedad pública para exponer la conexión
        public SqlConnection Connection => _connection;

        public async Task<string> GetSQLServerInfoAsync()
        {
            string result = string.Empty;
            var sql = @"
                SELECT 
                    'Connected to SQL Server: ' + @@SERVERNAME +
                    ', Version: ' + CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR) +
                    ', Edition: ' + CAST(SERVERPROPERTY('Edition') AS NVARCHAR) +
                    ', UTC Time: ' + CONVERT(NVARCHAR, GETUTCDATE(), 120)
            ";

            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();

            using var command = new SqlCommand(sql, _connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result = reader.GetString(0);
            }

            return result;
        }

        public async Task<int> InsertNotificationAsync(string title, string body)
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
            var cmdN = new SqlCommand(
                "INSERT INTO Notifications (Title, Body, CreatedByUserId) OUTPUT INSERTED.NotificationId VALUES (@t,@b,1)",
                _connection);
            cmdN.Parameters.AddWithValue("@t", title);
            cmdN.Parameters.AddWithValue("@b", body);

            var result = await cmdN.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                //_logger.LogError("Failed to insert notification and retrieve ID. Title='{Title}'", title);
                throw new Exception("Failed to insert notification and retrieve NotificationId.");
            }
            int notificationId = Convert.ToInt32(result);
            return notificationId;
        }

        public async Task InsertNotificationQueueAsync(int notificationId, int deviceId)
        {
            if (_connection.State != System.Data.ConnectionState.Open)
                await _connection.OpenAsync();
            var cmdQ = new SqlCommand(
                "INSERT INTO NotificationQueue (NotificationId, DeviceId) VALUES (@nid,@did)",
                _connection);
            cmdQ.Parameters.AddWithValue("@nid", notificationId);
            cmdQ.Parameters.AddWithValue("@did", deviceId);
            await cmdQ.ExecuteNonQueryAsync();
        }
    }
}