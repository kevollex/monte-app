using System;
using System.Net;

namespace MonteApp.ApiService.Models;

public class SessionInfo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CsrfToken { get; set; } = "";
    public CookieCollection? Cookies { get; set; }
    // Add other fields if needed
}
