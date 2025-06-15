using System.Net;
using MonteApp.ApiService.Infrastructure;
using MonteApp.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.AddSqlServerClient(connectionName: "monteappdb");
builder.Services.AddScoped<IDatabase, Database>();
builder.Services.AddScoped<ISubSystemsService, SubSystemsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");

app.MapControllers();
app.MapDefaultEndpoints();

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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
