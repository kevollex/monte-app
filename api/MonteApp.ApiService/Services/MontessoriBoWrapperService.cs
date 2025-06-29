using System;
using System.IdentityModel.Tokens.Jwt;
using HtmlAgilityPack;
using MonteApp.ApiService.Infrastructure;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Services;

public interface IMontessoriBoWrapperService
{
    // Define methods that will be implemented by the service
    // For example:
    Task<HomeData> GetHomeDataAsync(string sessionId);
    // Task<string> GetLicensesAsync();
}
public class MontessoriBoWrapperService : IMontessoriBoWrapperService
{
    private readonly IMontessoriBoWebsite _montessoriBoWebsite;
    private readonly IDatabase _database;

    public MontessoriBoWrapperService(IMontessoriBoWebsite montessoriBoWebsite,
                                      IDatabase database)
    {
        _montessoriBoWebsite = montessoriBoWebsite ?? throw new ArgumentNullException(nameof(montessoriBoWebsite));
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<HomeData> GetHomeDataAsync(string sessionId)
    {
        User? user = await _database.GetUserBySessionIdAsync(sessionId);
        HomeData homeData = new HomeData(
            user?.FullName ?? "Unknown User",
            new[]
            {
                new SubSystem("Licencias", "/licencias"),
                // new SubSystem("Control Semanal", "/control-semanal"),
                // new SubSystem("Ausencias", "/ausencias"),
                // new SubSystem("Notas", "/notas"),
                // new SubSystem("Comunicados", "/comunicados")
            }
        );

        return homeData;
    }
}

public record HomeData(string Username, SubSystem[] SubSystems );
public record SubSystem(string Name, string Route);