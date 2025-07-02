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
    Task<string> GetLicenciasAlumnosAsync(string idAlumno, string sessionId);
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
                new SubSystem("Licencias", "subsystem"),
                // new SubSystem("Control Semanal", "/control-semanal"),
                // new SubSystem("Ausencias", "/ausencias"),
                // new SubSystem("Notas", "/notas"),
                // new SubSystem("Comunicados", "/comunicados")
            }
        );

        return homeData;
    }

    public async Task<string> GetLicenciasAlumnosAsync(string idAlumno, string sessionId)
    {
        string result;
        string url = $"{Constants.SubsysLicencias_LicenciasAlumnosUrl}{idAlumno}";
        var response = await _montessoriBoWebsite.GetStringAsync(url, true);
        result = response;

        return result;
    }

    public async Task<string> GetLicenciasPageAsync(string sessionId)
    {
        string result;

        var responseContent = await _montessoriBoWebsite.GetStringAsync(Constants.SubsysLicenciasUrl, sessionId: sessionId);
        // Prepend this URL to every src/href (relative only)
        const string baseUrl = Constants.SubsysLicenciasUrl + "/"; ;
        const string fixedAjaxUrl = "api/proxy/licencias-alumnos"; // TODO: Better routing

        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
        htmlDoc.LoadHtml(responseContent);

        // Update <script src="...">
        foreach (var script in htmlDoc.DocumentNode.SelectNodes("//script[@src]") ?? Enumerable.Empty<HtmlNode>())
        {
            var src = script.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src) && !Uri.IsWellFormedUriString(src, UriKind.Absolute))
            {
                script.SetAttributeValue("src", baseUrl + src.TrimStart('/'));
            }
        }

        // Update <link href="...">
        foreach (var link in htmlDoc.DocumentNode.SelectNodes("//link[@href]") ?? Enumerable.Empty<HtmlNode>())
        {
            var href = link.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href) && !Uri.IsWellFormedUriString(href, UriKind.Absolute))
            {
                link.SetAttributeValue("href", baseUrl + href.TrimStart('/'));
            }
        }

        // Update <img src="...">
        foreach (var img in htmlDoc.DocumentNode.SelectNodes("//img[@src]") ?? Enumerable.Empty<HtmlNode>())
        {
            var src = img.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src) && !Uri.IsWellFormedUriString(src, UriKind.Absolute))
            {
                img.SetAttributeValue("src", baseUrl + src.TrimStart('/'));
            }
        }

        // Modify the GET licencias_alumnos AJAX request URL in inline script, appending sessionId
        foreach (var script in htmlDoc.DocumentNode.SelectNodes("//script[not(@src)]") ?? Enumerable.Empty<HtmlNode>())
        {
            var scriptText = script.InnerHtml;
            // Replace only the ajax url for licencias_alumnos.php
            scriptText = System.Text.RegularExpressions.Regex.Replace(
                scriptText,
                @"url\s*:\s*['""]licencias_alumnos\.php\?id='\s*\+\s*idalumno",
                "url: '" + fixedAjaxUrl + "?id='+idalumno+'&sessionId=" + sessionId + "'"
            );
            script.InnerHtml = scriptText;
        }

        result = htmlDoc.DocumentNode.OuterHtml;

        return result;
    }
}

public record HomeData(string Username, SubSystem[] SubSystems );
public record SubSystem(string Name, string Route);