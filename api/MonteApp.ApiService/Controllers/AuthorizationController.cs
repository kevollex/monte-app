using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    // [Route("api/v1/[controller]")] // TODO: Better routing strategy
    [ApiController]
    public class AuthorizationController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;
        public AuthorizationController(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request) // TODO: From auth headers, not body
        {
            string result = string.Empty;

            result = await _authorizationService.LoginAsync(request.Email, request.Password);

            return Ok(result);
        }
    }

    public record LoginRequest(string Email, string Password);
}
