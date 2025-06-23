using System;
using System.Net;

namespace MonteApp.ApiService.Infrastructure;

public interface IMontessoriBoWebsite
{
    Task<HttpResponseMessage> LoginAsync(string email, string password);
    Task<string> GetLicenciasPoCAsync();
}

// TODO: Inject HTTP client
public class MontessoriBoWebsite : IMontessoriBoWebsite
{
    public const string BaseUrl = "https://montessori.bo/principal/public";
    public const string LoginUrl = $"{BaseUrl}/login";
    public const string SistemaPadresId = "2";
    public const string LoginPadresUrl = $"{LoginUrl}?sistema={SistemaPadresId}";
    public const string SubsysLicenciasUrl = $"{BaseUrl}/LicenciasP";
    public const string SubsysLicencias_LicenciasAlumnosUrl = $"{SubsysLicenciasUrl}/licencias_alumnos.php?id=";

    // TODO: Inject HTTP client via constructor
    public MontessoriBoWebsite()
    {
    }

    public async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };

        using var client = new HttpClient(handler);
        // 1. Get the login page to retrieve the CSRF token
        var loginPage = await client.GetStringAsync(LoginPadresUrl);

        // 2. Extract CSRF token from the HTML (simplified, use regex or HTML parser)
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(loginPage, "name=\"_token\" value=\"([^\"]+)\"");
        var csrfToken = tokenMatch.Groups[1].Value;

        // 3. Prepare form data
        var formData = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("sistema", SistemaPadresId),
                new KeyValuePair<string, string>("_token", csrfToken)
            });

        // 4. Send POST request to login
        return await client.PostAsync(LoginUrl, formData);
    }

    public Task<string> GetLicenciasPoCAsync()
    {
        throw new NotImplementedException();
    }

}
