using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MonteApp.ApiService.Infrastructure;
using MonteApp.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add symmetric JWT authentication. TODO: Asymmetric for production
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.")
                )
            )
        };
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Dependencies injection
// Clients
builder.AddSqlServerClient(connectionName: "monteappdb");
builder.Services.AddScoped<CookieContainer>();
builder.Services.AddScoped(sp =>
{
    var cookieContainer = sp.GetRequiredService<CookieContainer>();
    return new HttpClient(new HttpClientHandler
    {
        CookieContainer = cookieContainer,
        UseCookies = true
    }, disposeHandler: true);
});
// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubSystemsService, SubSystemsService>();
// Infrastructure
builder.Services.AddScoped<IDatabase, Database>();
builder.Services.AddScoped<IMontessoriBoWebsite, MontessoriBoWebsite>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapControllers();
app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

// TODO: Move this method to a service class
async Task<string> LoginAndGetLicenciasPoCAsync(string baseUrl, string email, string password, int sistema = 2)
{
    var handler = new HttpClientHandler
    {
        CookieContainer = new CookieContainer(),
        UseCookies = true
    };

    using (var client = new HttpClient(handler))
    {
        // 1. Get the login page to retrieve the CSRF token
        var loginPage = await client.GetStringAsync($"{baseUrl}/login?sistema={sistema}");
        var padresUrl = $"{baseUrl}/padres";

        // 2. Extract CSRF token from the HTML (simplified, use regex or HTML parser)
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(loginPage, "name=\"_token\" value=\"([^\"]+)\"");
        var csrfToken = tokenMatch.Groups[1].Value;

        // 3. Prepare form data
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("email", email),
            new KeyValuePair<string, string>("password", password),
            new KeyValuePair<string, string>("sistema", sistema.ToString()),
            new KeyValuePair<string, string>("_token", csrfToken)
        });

        // 4. Send POST request to login
        var response = await client.PostAsync($"{baseUrl}/login", formData);

        // 5. Check if login was successful
        if (response.IsSuccessStatusCode)
        {
            // Cookies persisted in the handler will be used in subsequent requests, that's why we don't need to set them manually.
            // 6. Now we can access the protected pages
            var padresPage = await client.GetStringAsync(padresUrl);
            var licenciasPage = await client.GetStringAsync("https://montessori.bo/LicenciasP/");

            // Extract idAlumno from licenciasPage's cmbHijos select
            var idAlumnoMatch = System.Text.RegularExpressions.Regex.Match(
                licenciasPage,
                @"<select[^>]*id\s*=\s*[""']cmbHijos[""'][^>]*>.*?<option\s+value\s*=\s*[""']?([^'"" >]+)[""']?",
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            string idAlumno = idAlumnoMatch.Success ? idAlumnoMatch.Groups[1].Value : null;

            if (string.IsNullOrEmpty(idAlumno))
                return "No idAlumno found in cmbHijos!";

            var licenciasDataAlumno = await client.GetStringAsync("https://montessori.bo/LicenciasP/licencias_alumnos.php?id=" + idAlumno);

            return licenciasDataAlumno;
        }
        else
        {
            // Handle login failure
            return "Login failed: " + response.ReasonPhrase;
        }
        return null;
    }
}