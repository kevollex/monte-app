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
// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMontessoriBoWrapperService, MontessoriBoWrapperService>();
// Infrastructure
builder.AddSqlServerClient(connectionName: "monteappdb");
builder.Services.AddScoped<IDatabase, Database>();
builder.Services.AddScoped<CookieContainer>();
builder.Services.AddScoped<IMontessoriBoWebsite, MontessoriBoWebsite>(sp =>
{
    var cookieContainer = sp.GetRequiredService<CookieContainer>();
    var client = new HttpClient(new HttpClientHandler
    {
        CookieContainer = cookieContainer,
        UseCookies = true
    }, disposeHandler: false);
    return new MontessoriBoWebsite(client, cookieContainer);
});

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
