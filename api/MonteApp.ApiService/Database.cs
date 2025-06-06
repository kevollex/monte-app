using Microsoft.Data.SqlClient;

interface IDatabase
{
    Task<string> GetSQLServerInfoAsync();
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
}
