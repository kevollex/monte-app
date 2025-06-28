using System;
using System.Net;
using HtmlAgilityPack;

namespace MonteApp.ApiService.Infrastructure;

public interface IMontessoriBoWebsite
{
    Task<HttpResponseMessage> LoginAsync(string email, string password);
    Task<string> GetLicenciasPoCAsync();
}

// TODO: Inject HTTP client
public class MontessoriBoWebsite : IMontessoriBoWebsite
{
    private readonly HttpClient _client;
    public const string BaseUrl = "https://montessori.bo/principal/public";
    public const string LoginUrl = $"{BaseUrl}/login";
    public const string SistemaPadresId = "2";
    public const string LoginPadresUrl = $"{LoginUrl}?sistema={SistemaPadresId}";
    public const string SubsysLicenciasUrl = $"{BaseUrl}/LicenciasP";
    public const string SubsysLicencias_LicenciasAlumnosUrl = $"{SubsysLicenciasUrl}/licencias_alumnos.php?id=";

    // TODO: Inject HTTP client via constructor
    public MontessoriBoWebsite(HttpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        // 1. Get the login page to retrieve the CSRF token from content
        var loginPage = await _client.GetStringAsync(LoginPadresUrl);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(loginPage);

        // // 2. Extract CSRF token html node
        var tokenNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']") ?? throw new InvalidOperationException("CSRF token not found in the login page.");

        // 3. Prepare form data
        var formData = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("sistema", SistemaPadresId),
                new KeyValuePair<string, string>("_token", tokenNode.GetAttributeValue("content", ""))
            });

        // 4. Send POST request to login
        return await _client.PostAsync(LoginUrl, formData);
    }

    public Task<string> GetLicenciasPoCAsync()
    {
        throw new NotImplementedException();
    }

}
