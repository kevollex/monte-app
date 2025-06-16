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

        result = await _montessoriBoWebsite.LoginAsync(email, password);
        if (string.IsNullOrEmpty(result))
        {
            throw new UnauthorizedAccessException("Login failed. Please check your credentials.");
        }

        // string token = GenerateJWTToken(email, result); // TODO: Maybe?
        string token = GenerateJWTToken(email);
        // TODO: Store the token in a secure place, e.g., database or cache
        // For now, just return the token
        result = token;

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
        var keyVal = $"""
            MIHcAgEBBEIBL2wt/h7Whzcuyp1HiCNY1xKCOj1zgTUY3WNrCvaew+RnmAUJVTSw
            RO7uZvlAfRMwsiGZqJgVJ0OmR0xTQhzlI7egBwYFK4EEACOhgYkDgYYABACjXMjc
            1464NsMJlTFHyyzh5RLfzZy0xf9ScvM8Ibv0fAw61f2edRgOerxiX5ByrvZb/5tZ
            PBlsQlHHBaCrhIy/+QBLmy4U4o2P6VmXThPUUWDVigZkaQtfqg3oJhsg+bR/01oQ
            Lo5RXgs4JkPbA1eJA0rwvV5WCvRwcAbbsY5pyBM7Ng==
            """;
        // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])); // TODO: Configurable
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyVal));
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
