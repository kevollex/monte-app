using System;
using System.Net;

namespace MonteApp.ApiService.Infrastructure;

public interface IMontessoriBoWebsite
{
    Task<string> LoginAsync(string email, string password);
    Task<string> GetLicenciasPoCAsync();
}

// TODO: Inject HTTP client
public class MontessoriBoWebsite : IMontessoriBoWebsite
{
    // public const string LoginFormUrl = $"https://montessori.bo/login";
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

    public async Task<string> LoginAsync(string email, string password)
    {
        string result = string.Empty;

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
        var response = await client.PostAsync(LoginUrl, formData);

        // 5. Check if login was successful
        if (response.IsSuccessStatusCode)
        {
            // 6. Read the response content
            var responseContent = await response.Content.ReadAsStringAsync();

            // 7. Check if the login was successful by looking for a specific string in the response
            // This is a simplified check; you might want to use a more robust method
            // such as checking for a specific element or URL that indicates a successful login.
            // For example, checking if the response contains a specific title or element (div id="app")
            if (responseContent.Contains("Montessori - Portal para padres de familia"))
            {
                // Login successful, return session ID or any other relevant information
                result = "Login successful";
            }
        }
        else
        {
            // Handle login failure
            result = "Login failed: " + response.ReasonPhrase;
        }
        
        return result;
    }

    public Task<string> GetLicenciasPoCAsync()
    {
        throw new NotImplementedException();
    }

}
