using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Infrastructure;

public interface IDatabase
{
    Task<int> FindOrCreateUserAsync(string email, string password, string fullName);
    Task <int> UpsertSessionAsync(SessionInfo sessionInfo);
    Task<SessionInfo> GetSessionByIdAsync(string id);
    Task RevokeSessionAsync(string id);
    Task<User> GetUserBySessionIdAsync(string sessionId);
}

public class Database(SqlConnection connection) : IDatabase
{
    private readonly SqlConnection _connection = connection;

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
}
