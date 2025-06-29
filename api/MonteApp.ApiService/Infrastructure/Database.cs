using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Infrastructure;

public interface IDatabase
{
    Task<int> FindOrCreateUserAsync(string email, string password, string fullName);
    Task<int> CreateSessionAsync(int userId, string jwtId, string csrfToken, DateTime expiresAt, CookieCollection cookies);
    Task<SessionInfo?> GetSessionByIdAsync(string id);
    Task RevokeSessionAsync(string id);
    Task <User> GetUserBySessionIdAsync(string sessionId);
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

    public async Task<int> CreateSessionAsync(int userId, string jwtId, string csrfToken, DateTime expiresAt, CookieCollection cookies)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand(@"
            INSERT INTO sessions (user_id, csrf_token, jwt_id, created_at, expires_at, cookies)
            OUTPUT INSERTED.id
            VALUES (@UserId, @CsrfToken, @JwtId, GETUTCDATE(), @ExpiresAt, @Cookies)
        ", _connection);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@CsrfToken", csrfToken);
        cmd.Parameters.AddWithValue("@JwtId", jwtId);
        cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);
        cmd.Parameters.AddWithValue("@Cookies", SerializeCookies(cookies));

        await cmd.ExecuteNonQueryAsync();

        // Get the session id
        cmd.CommandText = "SELECT TOP 1 id FROM sessions WHERE jwt_id = @JwtId";
        var sessionId = await cmd.ExecuteScalarAsync();
        return (int)sessionId;
    }


    public async Task<SessionInfo?> GetSessionByIdAsync(string id)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand("SELECT id, user_id, csrf_token, cookies FROM sessions WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SessionInfo
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                CsrfToken = reader.GetString(2),
                Cookies = reader.IsDBNull(3) ? null : DeserializeCookies(reader.GetString(3))
            };
        }
        return null;
    }

    public async Task RevokeSessionAsync(string id)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand("UPDATE sessions SET revoked_at = GETUTCDATE() WHERE id = @id", _connection);
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
}
