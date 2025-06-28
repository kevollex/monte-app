using Microsoft.Data.SqlClient;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Infrastructure;

public interface IDatabase
{
    Task<string> GetSQLServerInfoAsync();
    Task<int> FindOrCreateUserAsync(string email, string password, string fullName);
    Task<int> CreateSessionAsync(int userId, string jwtId, string csrfToken, DateTime expiresAt);
    Task<SessionInfo?> GetSessionByIdAsync(string id);
    Task RevokeSessionAsync(string id);
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

    public async Task<int> CreateSessionAsync(int userId, string jwtId, string csrfToken, DateTime expiresAt)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var cmd = new SqlCommand(@"
            INSERT INTO sessions (user_id, csrf_token, jwt_id, created_at, expires_at)
            OUTPUT INSERTED.id
            VALUES (@UserId, @CsrfToken, @JwtId, GETUTCDATE(), @ExpiresAt)
        ", _connection);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@CsrfToken", csrfToken);
        cmd.Parameters.AddWithValue("@JwtId", jwtId);
        cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt);

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

        var cmd = new SqlCommand("SELECT id, user_id, csrf_token FROM sessions WHERE id = @id", _connection);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SessionInfo
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                CsrfToken = reader.GetString(2)
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
}
