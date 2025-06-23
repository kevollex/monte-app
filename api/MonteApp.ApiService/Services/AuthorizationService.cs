using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MonteApp.ApiService.Infrastructure;

namespace MonteApp.ApiService.Services;

public interface IAuthorizationService
{
    Task<string> LoginAsync(string email, string password);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly IMontessoriBoWebsite _montessoriBoWebsite;
    private readonly IConfiguration _config;

    public AuthorizationService(IMontessoriBoWebsite montessoriBoWebsite,
                                IConfiguration config)
    {
        _montessoriBoWebsite = montessoriBoWebsite ?? throw new ArgumentNullException(nameof(montessoriBoWebsite));
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

            // 7. Check if the login was successful by looking for a specific string in the response
            // This is a simplified check; you might want to use a more robust method
            // such as checking for a specific element or URL that indicates a successful login.
            // For example, checking if the response contains a specific title or element (div id="app")
            if (!responseContent.Contains("Montessori - Portal para padres de familia"))
            {
                // Login Succesful, now parse HTML response to get
                // Login successful
                throw new UnauthorizedAccessException("Login failed. Please check your credentials.");
            }
            // Login successful, now we can proceed to MonteApp
            string token = GenerateJWTToken(email);
            result = token;
        }
        else
        {
            throw new UnauthorizedAccessException("Login failed. Please check your credentials.");
        }

        // string token = GenerateJWTToken(email, result); // TODO: Maybe?
        // TODO: Store the token in a secure place, e.g., database or cache
        // For now, just return the token

        return result;
    }

    private string GenerateJWTToken(string email)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(ClaimTypes.Name, email),
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
            expires: DateTime.UtcNow.AddHours(1), // TODO: Make configurable
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
