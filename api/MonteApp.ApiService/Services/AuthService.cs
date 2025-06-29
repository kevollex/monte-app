using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using HtmlAgilityPack;
using Microsoft.IdentityModel.Tokens;
using MonteApp.ApiService.Infrastructure;
using MonteApp.ApiService.Models;

namespace MonteApp.ApiService.Services;

public interface IAuthService
{
    Task<string> LoginAsync(string email, string password);
    Task LogoutAsync(string sessionId);
}

public class AuthService : IAuthService
{
    private readonly IMontessoriBoWebsite _montessoriBoWebsite;
    private readonly IDatabase _database;
    private readonly IConfiguration _config;

    public AuthService(IMontessoriBoWebsite montessoriBoWebsite,
                                IDatabase database,
                                IConfiguration config)
    {
        _montessoriBoWebsite = montessoriBoWebsite ?? throw new ArgumentNullException(nameof(montessoriBoWebsite));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task<string> LoginAsync(string email, string password)
    {
        // NOW
        // 1. Login to MontessoriBo
        // 2. Login MontessoriBo OK? Login to MonteApp
        // LATER TODO: First to MonteApp, then to MontessoriBo. If MontessoriBo is down, allow login to MonteApp but with limited features
        string result;

        HttpResponseMessage response = await _montessoriBoWebsite.LoginAsync(email, password);
        // Compare cookies from login OK with login not Ok
        // 5. Check if login was successful
        if (response.IsSuccessStatusCode)
        {
            // 6. Read the response content
            var responseContent = await response.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(responseContent);

            var appDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='app']") ?? throw new UnauthorizedAccessException("Login failed. Please check your credentials.");

            // Login Success. Crawl padres page for data
            // No need to get or set cookies here, they are already set in the HttpClientHandler used by MontessoriBoWebsite for the request lifetime
            var padresPage = await _montessoriBoWebsite.GetPadresPageAsync();
            var padresPageContent = await padresPage.Content.ReadAsStringAsync();
            var padresHtmlDoc = new HtmlDocument();
            padresHtmlDoc.LoadHtml(padresPageContent);

            var padreFullNameNode = padresHtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'info')]/a[contains(@class,'d-block')]");
            var padreFullNameValue = padreFullNameNode?.InnerText.Trim() ?? throw new InvalidOperationException("Padre full name not found in the padres page.");

            var tokenNode = padresHtmlDoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']");
            var tokenValue = tokenNode?.GetAttributeValue("content", "") ?? throw new InvalidOperationException("CSRF token not found in the padres page.");

            var jwtId = Guid.NewGuid().ToString(); // Generate a unique JWT ID for the session. TODO: Store the jwt

            int userId = await _database.FindOrCreateUserAsync(email, password, padreFullNameValue);
            DateTime expiresAt = DateTime.UtcNow.AddHours(48); // TODO: Make configurable, decrease
            CookieCollection cookies = _montessoriBoWebsite.GetCookies();
            int sessionId = await _database.CreateSessionAsync(userId, jwtId, tokenValue, expiresAt, cookies);

            // Login successful, now we can proceed to MonteApp
            // Generate JWT with sessionId as jti
            string token = GenerateJWTToken(email, sessionId.ToString());
            result = token;
        }
        else
        {
            throw new UnauthorizedAccessException("Login failed. Please check your credentials.");
        }

        return result;
    }

    public async Task LogoutAsync(string sessionId)
    {
        // Lookup session in DB
        SessionInfo? session = await _database.GetSessionByIdAsync(sessionId) ?? throw new UnauthorizedAccessException("Session not found.");

        // Restore cookies from session
        if (session.Cookies != null)
        {
            _montessoriBoWebsite.SetCookies(session.Cookies);
        }

        // Perform logout via MontessoriBoWebsite using session.csrf_token
        var logoutResponse = await _montessoriBoWebsite.LogoutAsync(session.CsrfToken);
        if (!logoutResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to logout from MontessoriBo.");
        }

        // Mark session as revoked in DB
        await _database.RevokeSessionAsync(sessionId);
    }

    private string GenerateJWTToken(string email, string jwtId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(ClaimTypes.Name, email),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            // Add more claims as needed
        };
      
        var jwtKey = _config["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT key is not configured.");
        }
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(48), // TODO: Make configurable, decrease
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
