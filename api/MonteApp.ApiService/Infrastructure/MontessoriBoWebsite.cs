using System;
using System.Net;
using HtmlAgilityPack;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Infrastructure;

public interface IMontessoriBoWebsite
{
    Task<string> LoginAsync(string email, string password);
    Task LogoutAsync(string sessionId);
    Task<string> GetStringAsync(string url, bool skipSessionUpsert = false, string sessionId = "");
    CookieCollection GetCookies();
}

// TODO: Inject HTTP client
public class MontessoriBoWebsite : IMontessoriBoWebsite
{
    private readonly HttpClient _client;
    private readonly CookieContainer _cookieContainer;
    private readonly IDatabase _database;

    public MontessoriBoWebsite(HttpClient client,
                               CookieContainer cookieContainer,
                               IDatabase database)
    {
        _cookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public CookieCollection GetCookies()
    {
        return _cookieContainer.GetCookies(new Uri(Constants.HomeUrl));
    }

    private void SetCookies(CookieCollection cookies)
    {
        ArgumentNullException.ThrowIfNull(cookies);

        foreach (Cookie cookie in cookies)
        {
            _cookieContainer.Add(new Uri(Constants.HomeUrl), cookie);
        }
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        // 1. Get the login page to retrieve the CSRF token from content
        var loginPage = await _client.GetStringAsync(Constants.LoginPadresUrl);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(loginPage);

        // // 2. Extract CSRF token html node
        var tokenNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']") ?? throw new InvalidOperationException("CSRF token not found in the login page.");

        // 3. Prepare form data
        var formData = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("sistema", Constants.SistemaPadresId),
                new KeyValuePair<string, string>("_token", tokenNode.GetAttributeValue("content", ""))
            });

        // 4. Send POST request to login
        var response = await _client.PostAsync(Constants.LoginUrl, formData);
        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException($"Login failed: {response.ReasonPhrase}");
        }
        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }

    public async Task LogoutAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty.", nameof(sessionId));
        }

        SessionInfo session = await _database.GetSessionByIdAsync(sessionId) ?? throw new UnauthorizedAccessException("Session not found.");
        if (session.Cookies != null)
        {
            SetCookies(session.Cookies);
        }

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("_token", session.CsrfToken),
        });

        var response = await _client.PostAsync(Constants.LogoutUrl, formData);
        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException($"Logout failed: {response.ReasonPhrase}");
        }
    }

    // Get any page/content from MontessoriBo
    public async Task<string> GetStringAsync(string url, bool skipSessionUpsert = false, string sessionId = "")
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }
        
        SessionInfo session = new SessionInfo();
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            session = await _database.GetSessionByIdAsync(sessionId);
            if (session.Cookies != null)
            {
                SetCookies(session.Cookies);
            }
        }
        
        string result;
        var response = await _client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException($"Error retrieving data from {url}: {response.ReasonPhrase}");
        }
        result = await response.Content.ReadAsStringAsync();

        if (!skipSessionUpsert)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(result);
            var tokenNode = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']");
            string csrfToken = tokenNode != null ? tokenNode.GetAttributeValue("content", "") : string.Empty;
            if (!string.IsNullOrEmpty(csrfToken))
            {
                session.CsrfToken = csrfToken;
            }
            session.Cookies = GetCookies();

            await _database.UpsertSessionAsync(session);
        }

        return result;
    }
}
