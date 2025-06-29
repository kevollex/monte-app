using System;
using System.Net;
using HtmlAgilityPack;

namespace MonteApp.ApiService.Infrastructure;

public interface IMontessoriBoWebsite
{
    Task<HttpResponseMessage> LoginAsync(string email, string password);
    Task<HttpResponseMessage> LogoutAsync(string csrfToken);
    Task<HttpResponseMessage> GetPadresPageAsync();
    Task<HttpResponseMessage> GetLicenciasPageAsync();

    CookieCollection GetCookies();
    void SetCookies(CookieCollection cookies);
}

// TODO: Inject HTTP client
public class MontessoriBoWebsite : IMontessoriBoWebsite
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer;
    public const string HomeUrl = "https://montessori.bo";
    public const string BaseUrl = $"{HomeUrl}/principal/public";
    // public const string BaseUrl = "https://montessori.bo/principal/public";
    public const string LoginUrl = $"{BaseUrl}/login";
    public const string LogoutUrl = $"{BaseUrl}/logout";
    public const string SistemaPadresId = "2";
    public const string LoginPadresUrl = $"{LoginUrl}?sistema={SistemaPadresId}";
    public const string PadresUrl = $"{BaseUrl}/padres";
    public const string SubsysLicenciasUrl = $"{HomeUrl}/LicenciasP";
    public const string SubsysLicencias_LicenciasAlumnosUrl = $"{SubsysLicenciasUrl}/licencias_alumnos.php?id=";

    // TODO: Inject HTTP client via constructor
    public MontessoriBoWebsite(HttpClient client,
                              CookieContainer cookieContainer)
    {
        _cookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public CookieCollection GetCookies()
    {
        return _cookieContainer.GetCookies(new Uri(HomeUrl));
    }

    public void SetCookies(CookieCollection cookies)
    {
        ArgumentNullException.ThrowIfNull(cookies);

        foreach (Cookie cookie in cookies)
        {
            _cookieContainer.Add(new Uri(HomeUrl), cookie);
        }
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

    public async Task<HttpResponseMessage> LogoutAsync(string csrfToken)
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("_token", csrfToken)
        });

        return await _client.PostAsync(LogoutUrl, formData);
    }

    public async Task<HttpResponseMessage> GetPadresPageAsync()
    {
        return await _client.GetAsync(PadresUrl);
    }

    public async Task<HttpResponseMessage> GetLicenciasPageAsync()
    {
        return await _client.GetAsync(SubsysLicenciasUrl);
    }
}
