using System.Data;
using Microsoft.Data.SqlClient;

namespace MonteApp.ApiService.Infrastructure;

public interface IDatabase
{
    Task<string> GetSQLServerInfoAsync();
    Task<int> InsertNotificationAsync(string title, string body);
    Task InsertNotificationQueueAsync(int notificationId, int deviceId);
    Task<List<(int QueueId, int NotificationId, int DeviceId)>> GetPendingQueueAsync();
    Task<(string Title, string Body)> GetNotificationContentAsync(int notificationId);
    Task<string> GetDeviceTokenAsync(int deviceId);
    Task UpdateQueueStatusAsync(int queueId, string status, string? errorMessage);
    Task<int> UpsertDeviceAsync(string fcmToken);
    Task DeleteDeviceAsync(int deviceId);
}

class Database(SqlConnection connection) : IDatabase
{
    private readonly SqlConnection _connection = connection;

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
    if (_connection.State != ConnectionState.Open)
        await _connection.OpenAsync();

    // Ahora incluimos CreatedByUserId con un valor por defecto (por ejemplo 1)
    using var cmd = new SqlCommand(
        @"INSERT INTO Notifications
             (Title, Body, CreatedByUserId)
          OUTPUT INSERTED.NotificationId
          VALUES
             (@title, @body, @userId)",
        _connection);

    cmd.Parameters.AddWithValue("@title", title);
    cmd.Parameters.AddWithValue("@body",  body);
    cmd.Parameters.AddWithValue("@userId", 1);     // <<–– aquí damos un valor no nulo

    var result = await cmd.ExecuteScalarAsync();
    if (result == null || result == DBNull.Value)
        throw new Exception("Failed to insert notification.");

    return Convert.ToInt32(result);
}
    public async Task InsertNotificationQueueAsync(int notificationId, int deviceId)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(
            "INSERT INTO NotificationQueue (NotificationId, DeviceId) VALUES (@nid,@did)",
            _connection);
        cmd.Parameters.AddWithValue("@nid", notificationId);
        cmd.Parameters.AddWithValue("@did", deviceId);
        await cmd.ExecuteNonQueryAsync();
    }
public async Task<List<(int, int, int)>> GetPendingQueueAsync()
{
    if (_connection.State != ConnectionState.Open)
        await _connection.OpenAsync();

    var list = new List<(int, int, int)>();
    using var cmd = new SqlCommand(
      "SELECT QueueId, NotificationId, DeviceId FROM NotificationQueue WHERE Status = 'Pending'",
      _connection);
    using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
        list.Add((r.GetInt32(0), r.GetInt32(1), r.GetInt32(2)));
    return list;
}

public async Task<(string, string)> GetNotificationContentAsync(int nid)
{
    if (_connection.State != ConnectionState.Open)
        await _connection.OpenAsync();

    using var cmd = new SqlCommand(
      "SELECT Title, Body FROM Notifications WHERE NotificationId=@nid",
      _connection);
    cmd.Parameters.AddWithValue("@nid", nid);
    using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
        throw new Exception($"Notification {nid} not found.");
    return (r.GetString(0), r.GetString(1));
}

public async Task<string> GetDeviceTokenAsync(int did)
{
    if (_connection.State != ConnectionState.Open)
        await _connection.OpenAsync();

    using var cmd = new SqlCommand(
      "SELECT TokenFCM FROM Devices WHERE DeviceId=@did",
      _connection);
    cmd.Parameters.AddWithValue("@did", did);
    var tok = await cmd.ExecuteScalarAsync();
    if (tok == null || tok == DBNull.Value)
        throw new Exception($"Device {did} has no token.");
    return (string)tok;
}

    public async Task<int> UpsertDeviceAsync(string fcmToken)
{
    if (_connection.State != ConnectionState.Open)
        await _connection.OpenAsync();

    using var cmdFind = new SqlCommand(
        "SELECT DeviceId FROM Devices WHERE TokenFCM = @t", _connection);
    cmdFind.Parameters.AddWithValue("@t", fcmToken);
    var existing = await cmdFind.ExecuteScalarAsync();

    if (existing is int did)
    {
        return did;
    }
    else
    {
        // Aquí incluimos UserId con valor por defecto = 1
        using var cmdIns = new SqlCommand(
            @"INSERT INTO Devices (TokenFCM, UserId)
              OUTPUT INSERTED.DeviceId
              VALUES (@t, @u)", _connection);
        cmdIns.Parameters.AddWithValue("@t", fcmToken);
        cmdIns.Parameters.AddWithValue("@u", 1);  // <- valor por defecto

        var newId = await cmdIns.ExecuteScalarAsync();
        return Convert.ToInt32(newId!);
    }
}

    public async Task DeleteDeviceAsync(int deviceId)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(
          "DELETE FROM Devices WHERE DeviceId = @did", _connection);
        cmd.Parameters.AddWithValue("@did", deviceId);
        await cmd.ExecuteNonQueryAsync();
    }

    public Task UpdateQueueStatusAsync(int queueId, string status, string? errorMessage)
    {
        throw new NotImplementedException();
    }
}
