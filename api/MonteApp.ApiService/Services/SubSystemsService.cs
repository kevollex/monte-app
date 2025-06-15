using System;
using MonteApp.ApiService.Infrastructure;

namespace MonteApp.ApiService.Services;

public interface ISubSystemsService
{
    /// <summary>
    /// Gets information about the SQL Server instance.
    /// Params:
    /// - None
    /// Returns:
    /// - A string containing information about the SQL Server instance.
    /// </summary>
    Task<string> GetSQLServerInfoAsync();
    Task<string> LoginAndGetLicenciasPoCAsync(string url, string email, string password);
}

public class SubSystemsService : ISubSystemsService
{
    private readonly IDatabase _database;

    public SubSystemsService(IDatabase database)
    {
        this._database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<string> GetSQLServerInfoAsync()
    {
        string result = await _database.GetSQLServerInfoAsync();

        return result;
    }

    public Task<string> LoginAndGetLicenciasPoCAsync(string url, string email, string password)
    {
        throw new NotImplementedException();
    }
}
