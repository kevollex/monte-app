using System;
using HtmlAgilityPack;
using MonteApp.ApiService.Infrastructure;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Services;

public interface IMontessoriBoWrapperService
{
    Task<HomeData> GetHomeDataAsync(string sessionId);
    Task<string> GetPageAsync(string sessionId, string url);
    Task<string> GetLicenciasPageAsync(string sessionId, bool enableScheduleRestrictionBypass = false);
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
            user?.FullName ?? "Unknown User 🕵",
            new[]
            {
                new SubSystem("control-semanal"),
                new SubSystem("cartas-recibidas"),
                new SubSystem("circulares"),
                new SubSystem("licencias"),
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

    public async Task<string> GetLicenciasPageAsync(string sessionId, bool enableScheduleRestrictionBypass = false)
    {
        string result;

        var responseContent = await _montessoriBoWebsite.GetStringAsync(Constants.SubsysLicenciasUrl, sessionId: sessionId);
        // Prepend this URL to every src/href (relative only)
        const string baseUrl = Constants.SubsysLicenciasUrl + "/"; ;
        const string fixedAjaxUrl = "api/proxy/licencias/licencias-alumnos"; // TODO: Better routing
        const string fixedEnviaAjaxUrl = "api/proxy/licencias/licencia-envia"; // TODO: Better routing

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

            // Replace all occurrences of licencias_alumnos.php (with or without query params)
            var licenciasRegex = new System.Text.RegularExpressions.Regex(
                @"url\s*:\s*['""]licencias_alumnos\.php(?:\?id=['""]?\s*\+\s*idalumno)?['""]?",
                System.Text.RegularExpressions.RegexOptions.Compiled);

            var newScriptText = licenciasRegex.Replace(
                scriptText,
                "url: '" + fixedAjaxUrl + "?id='+idalumno+'&sessionId=" + sessionId + "'"
            );

            // Replace all occurrences of licencia_envia.php
            var enviaRegex = new System.Text.RegularExpressions.Regex(
                @"url\s*:\s*['""]licencia_envia\.php['""]",
                System.Text.RegularExpressions.RegexOptions.Compiled);

            newScriptText = enviaRegex.Replace(
                newScriptText,
                "url: '" + fixedEnviaAjaxUrl + "?sessionId=" + sessionId + "'"
            );

            if (scriptText != newScriptText)
            {
                var originalScript = script.OuterHtml;
                var newScript = originalScript.Replace(scriptText, newScriptText);
                replacements.Add((originalScript, newScript));
            }
        }

        if(enableScheduleRestrictionBypass)
        {
            var blockToComment = @"(?m)^[ \t]*if\s*\(\s*\(h>='07:00'\s*&&\s*h<'09:00'\)\s*\|\|\s*\(h>='12:00'\s*&&\s*h<'14:00'\)\s*\|\|\s*\(h>='18:00'\s*&&\s*h<'24:00'\)\)\s*\{[\s\S]*?\}[ \t\r\n]*else\s*\{[\s\S]*?\}[ \t\r\n]*";
            var blockRegex = new System.Text.RegularExpressions.Regex(blockToComment, System.Text.RegularExpressions.RegexOptions.Singleline);

            // Find the first match and comment out each line with //
            var match = blockRegex.Match(modifiedContent);
            if (match.Success)
            {
                var block = match.Value;
                // Comment out each line (preserve indentation)
                var commented = string.Join("\n", block.Split('\n').Select(line => line.Trim().Length > 0 ? "//" + line : line));

                // Now, uncomment just the four lines
                var linesToUncomment = new[]
                {
                "$('#nombreusuario').val(nombreUsuario);",
                "$('#idalumno').val(idalumno);",
                "$('#nombrehijo').val(nombrehijo);",
                "$('#formulario').dialog('open');"
                };

                foreach (var codeLine in linesToUncomment)
                {
                // Remove // only if it is at the start of the line (possibly after whitespace)
                commented = System.Text.RegularExpressions.Regex.Replace(
                    commented,
                    @"^(\s*)//\s*" + System.Text.RegularExpressions.Regex.Escape(codeLine),
                    "$1" + codeLine,
                    System.Text.RegularExpressions.RegexOptions.Multiline
                );
                }

                replacements.Add((block, commented));
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

    // TODO: Use regex to replace some urls in the HTML content
    //       - src/href attributes in <script>, <link>, <img> tags
    //       - AJAX URLs in inline scripts maybe?
    public async Task<string> GetPageAsync(string sessionId, string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        // Ensure the URL is absolute
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            throw new ArgumentException("The provided URL is not valid.", nameof(url));
        }

        // Fetch the page content
        var response = await _montessoriBoWebsite.GetStringAsync(url, sessionId: sessionId);

        return response;
    }
}

public record HomeData(string Username, SubSystem[] SubSystems );
public record SubSystem(string Label);