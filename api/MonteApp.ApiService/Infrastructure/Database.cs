using System.Data;
using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Infrastructure;

public interface IDatabase
{
    Task<int> FindOrCreateUserAsync(string email, string password, string fullName);
    Task<int> UpsertSessionAsync(SessionInfo sessionInfo);
    Task<SessionInfo> GetSessionByIdAsync(string id);
    Task RevokeSessionAsync(string id);
    Task<User> GetUserBySessionIdAsync(string sessionId);
    Task<int> UpsertDeviceAsync(string deviceToken);
    Task<List<(int QueueId, int NotificationId, int DeviceId)>> GetPendingQueueAsync();
    Task<(string Title, string Body)> GetNotificationContentAsync(int notificationId);
    Task UpdateQueueStatusAsync(int queueId, string status, string? errorMessage);
}


public class Database : IDatabase
{
    private readonly SqlConnection _connection;

    public Database(SqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<int> FindOrCreateUserAsync(string email, string password, string fullName)
    {
        int userId;

        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        // Try to find user
        var findCmd = new SqlCommand("SELECT id FROM users WHERE email = @Email", _connection);
        findCmd.Parameters.AddWithValue("@Email", email);
        var result = await findCmd.ExecuteScalarAsync();
        if (result != null)
        {
            userId = Convert.ToInt32(result);
            return userId; // User already exists, return their ID
        }

        // Create user
        var insertCmd = new SqlCommand(
            "INSERT INTO users (email, password_hash, full_name, is_active, created_at) OUTPUT INSERTED.id VALUES (@Email,@PasswordHash, @FullName, 1, GETUTCDATE())", _connection);
        insertCmd.Parameters.AddWithValue("@Email", email);
        insertCmd.Parameters.AddWithValue("@PasswordHash", "NOT-A-REAL-HASHED-PASSWORD"); // TODO: store passwords?
        insertCmd.Parameters.AddWithValue("@FullName", fullName);

        result = await insertCmd.ExecuteScalarAsync();
        userId = Convert.ToInt32(result);
        return userId; // Return the newly created user's ID
    }

    public async Task<SessionInfo> GetSessionByIdAsync(string id)
    {
        SessionInfo sessionInfo = new SessionInfo();

        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand("SELECT id, user_id, csrf_token, jwt_id, created_at, expires_at, revoked_at, cookies, updated_at FROM sessions WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            sessionInfo = new SessionInfo
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                CsrfToken = reader.GetString(2),
                JwtId = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                ExpiresAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                RevokedAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                Cookies = reader.IsDBNull(7) ? null : DeserializeCookies(reader.GetString(7)),
                UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            };
        }
        return sessionInfo;
    }

    public async Task RevokeSessionAsync(string id)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand("UPDATE sessions SET revoked_at = GETUTCDATE(), updated_at = GETUTCDATE() WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<User> GetUserBySessionIdAsync(string sessionId)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT u.id, u.email, u.full_name, u.created_at
            FROM sessions s
            INNER JOIN users u ON s.user_id = u.id
            WHERE s.id = @SessionId", _connection);
        cmd.Parameters.AddWithValue("@SessionId", sessionId);

        var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Email = reader.GetString(1),
                FullName = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        throw new InvalidOperationException("User not found.");
    }

    private static string SerializeCookies(CookieCollection cookies)
    {
        var cookieList = new List<object>();
        foreach (Cookie cookie in cookies)
        {
            cookieList.Add(new
            {
                cookie.Name,
                cookie.Value,
                cookie.Domain,
                cookie.Path,
                cookie.Expires,
                cookie.Secure,
                cookie.HttpOnly
            });
        }
        return JsonSerializer.Serialize(cookieList);
    }

    private static CookieCollection DeserializeCookies(string json)
    {
        var cookies = new CookieCollection();
        var cookieList = JsonSerializer.Deserialize<List<Cookie>>(json);
        if (cookieList != null)
        {
            foreach (var c in cookieList)
            {
                var cookie = new Cookie(c.Name, c.Value, c.Path, c.Domain)
                {
                    Expires = c.Expires,
                    Secure = c.Secure,
                    HttpOnly = c.HttpOnly
                };
                cookies.Add(cookie);
            }
        }
        return cookies;
    }

    public async Task<int> UpsertSessionAsync(SessionInfo sessionInfo)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand(@"
            MERGE INTO sessions AS target
            USING (SELECT @Id AS id) AS source
            ON (target.id = source.id)
            WHEN MATCHED THEN
                UPDATE SET
                    user_id = @UserId,
                    csrf_token = @CsrfToken,
                    jwt_id = @JwtId,
                    expires_at = @ExpiresAt,
                    revoked_at = NULL, -- Reset revoked_at on update,
                    cookies = @Cookies,
                    updated_at = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (user_id, csrf_token, jwt_id, created_at, expires_at, cookies)
                VALUES (@UserId, @CsrfToken, @JwtId, GETUTCDATE(), @ExpiresAt, @Cookies)
            OUTPUT inserted.id;
        ", _connection);

        cmd.Parameters.AddWithValue("@Id", sessionInfo.Id);
        cmd.Parameters.AddWithValue("@UserId", sessionInfo.UserId);
        cmd.Parameters.AddWithValue("@CsrfToken", sessionInfo.CsrfToken ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@JwtId", sessionInfo.JwtId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ExpiresAt", sessionInfo.ExpiresAt ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Cookies", sessionInfo.Cookies != null ? SerializeCookies(sessionInfo.Cookies) : (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        if (result != null && int.TryParse(result.ToString(), out int sessionId))
        {
            return sessionId;
        }
        else
        {
            throw new InvalidOperationException("Failed to upsert session.");
        }
    }
        public async Task<int> UpsertDeviceAsync(string deviceToken)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        // SI YA existe el token, devuelve su DeviceId; si no, lo inserta.
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

    public async Task<List<(int QueueId, int NotificationId, int DeviceId)>> GetPendingQueueAsync()
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

    public async Task UpdateQueueStatusAsync(int queueId, string status, string? errorMessage)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(@"
            UPDATE notification_queue
               SET status = @s
                 , sent_at = CASE WHEN @s = 'sent' THEN GETDATE() END
                 , error_message = @e
             WHERE queue_id = @qid", 
             _connection);
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@e", errorMessage ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@qid", queueId);
        await cmd.ExecuteNonQueryAsync();
    }
}
