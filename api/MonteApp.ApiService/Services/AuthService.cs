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

        var responseContent = await _montessoriBoWebsite.LoginAsync(email, password);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(responseContent);

        var appDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='app']") ?? throw new UnauthorizedAccessException("Login failed. Please check your credentials.");

        // Login Success. Crawl padres page for data
        var padresPageContent = await _montessoriBoWebsite.GetStringAsync(Constants.PadresUrl, skipSessionUpsert: true);
        var padresHtmlDoc = new HtmlDocument();
        padresHtmlDoc.LoadHtml(padresPageContent);

        var padreFullNameNode = padresHtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class,'info')]/a[contains(@class,'d-block')]");
        var padreFullNameValue = padreFullNameNode?.InnerText.Trim() ?? throw new InvalidOperationException("Padre full name not found in the padres page.");

        var csrfTokenNode = padresHtmlDoc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']");
        var csrfTokenValue = csrfTokenNode?.GetAttributeValue("content", "") ?? throw new InvalidOperationException("CSRF token not found in the padres page.");

        var jwtId = Guid.NewGuid().ToString(); // Generate a unique JWT ID for the session. TODO: Store the jwt???

        int userId = await _database.FindOrCreateUserAsync(email, password, padreFullNameValue);
        DateTime expiresAt = DateTime.UtcNow.AddHours(48); // TODO: Make configurable, decrease
        CookieCollection cookies = _montessoriBoWebsite.GetCookies();
        SessionInfo sessionInfo = new SessionInfo
        {
            UserId = userId,
            CsrfToken = csrfTokenValue,
            JwtId = jwtId,
            ExpiresAt = expiresAt,
            Cookies = cookies
        };
        int sessionId = await _database.UpsertSessionAsync(sessionInfo);

        // Login successful, now we can proceed to MonteApp
        // Generate JWT with sessionId as jti
        string token = GenerateJWTToken(email, sessionId.ToString());
        result = token;

        return result;
    }

    public async Task LogoutAsync(string sessionId)
    {
        await _montessoriBoWebsite.LogoutAsync(sessionId);

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
