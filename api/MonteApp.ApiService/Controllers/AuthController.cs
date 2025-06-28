using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    // [Route("api/v1/[controller]")] // TODO: Better routing strategy
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            string result = string.Empty;

            result = await _authService.LoginAsync(request.Email, request.Password);

            return Ok(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Grab the JWT from the Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized("Missing or invalid Authorization header.");

            var jwtToken = authHeader.Substring("Bearer ".Length).Trim();

            await _authService.LogoutAsync(jwtToken);

            return Ok("Logged out");
        }
    }

    public record LoginRequest(string Email, string Password);
}
