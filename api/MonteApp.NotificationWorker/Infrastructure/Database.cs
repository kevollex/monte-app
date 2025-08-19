using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace MonteApp.NotificationWorker.Infrastructure;

public interface IDatabase
{
    Task<int> UpsertDeviceAsync(string deviceToken);
    Task<List<(int QueueId, int NotificationId, int DeviceId)>> GetPendingQueueAsync();
    Task<string> GetDeviceTokenAsync(int deviceId);
    Task<(string Title, string Body)> GetNotificationContentAsync(int notificationId);
    Task UpdateQueueStatusAsync(int queueId, string status, string? errorMessage);
    Task<int> InsertNotificationAsync(string title, string body);
    Task InsertNotificationQueueAsync(int notificationId, int deviceId);
    Task<List<int>> GetDeviceIdsByUserIdAsync(int userId);
    
}

public class Database : IDatabase
{
    private readonly SqlConnection _connection;

    public Database([FromKeyedServices("monteappdb")] SqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<int> UpsertDeviceAsync(string deviceToken)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        // Si ya existe, devuelve su DeviceId; si no, lo inserta.
        using var find = new SqlCommand(
            "SELECT device_id FROM devices WHERE token_fcm = @t", _connection);
        find.Parameters.AddWithValue("@t", deviceToken);
        var existing = await find.ExecuteScalarAsync();
        if (existing != null && existing != DBNull.Value)
            return Convert.ToInt32(existing);

        using var ins = new SqlCommand(@"
                INSERT INTO devices (user_id, token_fcm, platform, is_active, registered_at)
                OUTPUT INSERTED.device_id
                VALUES (1, @t, NULL, 1, GETDATE());", _connection);
        ins.Parameters.AddWithValue("@t", deviceToken);
        var id = await ins.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task<List<(int, int, int)>> GetPendingQueueAsync()
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        var list = new List<(int, int, int)>();
        using var cmd = new SqlCommand(
            "SELECT queue_id, notification_id, device_id FROM notification_queue WHERE status = 'pending'",
            _connection);
        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add((r.GetInt32(0), r.GetInt32(1), r.GetInt32(2)));
        return list;
    }

    public async Task<(string Title, string Body)> GetNotificationContentAsync(int notificationId)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(
            "SELECT title, body FROM notifications WHERE notification_id = @nid", _connection);
        cmd.Parameters.AddWithValue("@nid", notificationId);
        using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync())
            throw new Exception($"Notification {notificationId} not found");
        return (r.GetString(0), r.GetString(1));
    }

    public async Task<string> GetDeviceTokenAsync(int deviceId)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(
            "SELECT token_fcm FROM devices WHERE device_id = @did", _connection);
        cmd.Parameters.AddWithValue("@did", deviceId);
        var tok = await cmd.ExecuteScalarAsync();
        if (tok == null || tok == DBNull.Value)
            throw new Exception($"Device {deviceId} has no token.");
        return (string)tok;
    }

    public async Task UpdateQueueStatusAsync(int queueId, string status, string? errorMessage)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(@"
                UPDATE notification_queue
                   SET status        = @s,
                       sent_at       = CASE WHEN @s = 'sent'   THEN GETDATE() END,
                       error_message = CASE WHEN @s = 'failed' THEN @e        END
                 WHERE queue_id = @qid",
             _connection);
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@e", errorMessage ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@qid", queueId);
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<int> InsertNotificationAsync(string title, string body)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(@"
            INSERT INTO notifications (created_by_user_id, type, title, body, created_at)
            OUTPUT INSERTED.notification_id
            VALUES (1, @type, @title, @body, GETDATE());", _connection);
        cmd.Parameters.AddWithValue("@type", "info");         // o el tipo que quieras
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@body", body);

        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id);
    }

    public async Task InsertNotificationQueueAsync(int notificationId, int deviceId)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(@"
            INSERT INTO notification_queue (notification_id, device_id, status, scheduled_at)
            VALUES (@nid, @did, 'pending', GETDATE());", _connection);
        cmd.Parameters.AddWithValue("@nid", notificationId);
        cmd.Parameters.AddWithValue("@did", deviceId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<int>> GetDeviceIdsByUserIdAsync(int userId)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        var ids = new List<int>();
        using var cmd = new SqlCommand(
            @"SELECT device_id
              FROM devices
             WHERE user_id   = @uid
               AND is_active = 1", 
            _connection);
        cmd.Parameters.AddWithValue("@uid", userId);

        using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            ids.Add(r.GetInt32(0));

        return ids;
    }
}