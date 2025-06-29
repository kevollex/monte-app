using System;
using System.IdentityModel.Tokens.Jwt;
using HtmlAgilityPack;
using MonteApp.ApiService.Infrastructure;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Services;

public interface IMontessoriBoWrapperService
{
    Task<HomeData> GetHomeDataAsync(string sessionId);
    Task<string> GetLicenciasPageAsync(string sessionId);
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

    public async Task<string> GetLicenciasPageAsync(string sessionId)
    {
        // Lookup session in DB
        SessionInfo? session = await _database.GetSessionByIdAsync(sessionId) ?? throw new UnauthorizedAccessException("Session not found.");

        // Restore cookies from session
        if (session.Cookies != null)
        {
            _montessoriBoWebsite.SetCookies(session.Cookies);
        }
        string result;

        HttpResponseMessage response = await _montessoriBoWebsite.GetLicenciasPageAsync();
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            result = responseContent;
        }
        else
        {
            throw new UnauthorizedAccessException("Error retrieving Licencias page: " + response.ReasonPhrase);
        }

        return result;
    }
}

public record HomeData(string Username, SubSystem[] SubSystems );
public record SubSystem(string Name, string Route);