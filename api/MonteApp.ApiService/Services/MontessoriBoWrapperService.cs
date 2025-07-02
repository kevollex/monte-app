using System;
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
        var response = await _montessoriBoWebsite.GetStringAsync(url, true, sessionId);
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

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseContent);

        // Instead of returning the parsed HTML, apply changes directly to the original string
        // 1. Track replacements as (original, replacement) pairs
        var replacements = new List<(string original, string replacement)>();

        // Collect <script src="...">
        foreach (var script in htmlDoc.DocumentNode.SelectNodes("//script[@src]") ?? Enumerable.Empty<HtmlNode>())
        {
            var src = script.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src) && !Uri.IsWellFormedUriString(src, UriKind.Absolute))
            {
            var newSrc = baseUrl + src.TrimStart('/');
            // Find the original attribute string in the HTML
            var originalAttr = $"src=\"{src}\"";
            var replacementAttr = $"src=\"{newSrc}\"";
            replacements.Add((originalAttr, replacementAttr));
            }
        }

        // Collect <link href="...">
        foreach (var link in htmlDoc.DocumentNode.SelectNodes("//link[@href]") ?? Enumerable.Empty<HtmlNode>())
        {
            var href = link.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href) && !Uri.IsWellFormedUriString(href, UriKind.Absolute))
            {
            var newHref = baseUrl + href.TrimStart('/');
            var originalAttr = $"href=\"{href}\"";
            var replacementAttr = $"href=\"{newHref}\"";
            replacements.Add((originalAttr, replacementAttr));
            }
        }

        // Collect <img src="...">
        foreach (var img in htmlDoc.DocumentNode.SelectNodes("//img[@src]") ?? Enumerable.Empty<HtmlNode>())
        {
            var src = img.GetAttributeValue("src", "");
            if (!string.IsNullOrEmpty(src) && !Uri.IsWellFormedUriString(src, UriKind.Absolute))
            {
            var newSrc = baseUrl + src.TrimStart('/');
            var originalAttr = $"src=\"{src}\"";
            var replacementAttr = $"src=\"{newSrc}\"";
            replacements.Add((originalAttr, replacementAttr));
            }
        }

        // For inline scripts, replace only the AJAX URL for licencias_alumnos.php
        string modifiedContent = responseContent;
        foreach (var script in htmlDoc.DocumentNode.SelectNodes("//script[not(@src)]") ?? Enumerable.Empty<HtmlNode>())
        {
            var scriptText = script.InnerHtml;
            var regex = new System.Text.RegularExpressions.Regex(
            @"url\s*:\s*['""]licencias_alumnos\.php\?id='\s*\+\s*idalumno",
            System.Text.RegularExpressions.RegexOptions.Compiled);

            if (regex.IsMatch(scriptText))
            {
            // Find the original script block in the HTML
            var originalScript = script.OuterHtml;
            var newScriptText = regex.Replace(
                scriptText,
                "url: '" + fixedAjaxUrl + "?id='+idalumno+'&sessionId=" + sessionId + "'"
            );
            var newScript = originalScript.Replace(scriptText, newScriptText);
            replacements.Add((originalScript, newScript));
            }
        }

        // Apply all replacements to the original HTML content
        foreach (var (original, replacement) in replacements)
        {
            modifiedContent = modifiedContent.Replace(original, replacement);
        }

        result = modifiedContent;

        return result;
    }
}

public record HomeData(string Username, SubSystem[] SubSystems );
public record SubSystem(string Name, string Route);