using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    // TODO: add [controller] and versioning to route
    [ApiController]
    [Authorize]
    public class SubSystemsController : ControllerBase
    {
        private readonly ISubSystemsService _subSystemsService;

        public SubSystemsController(ISubSystemsService subSystemsService)
        {
            _subSystemsService = subSystemsService ?? throw new ArgumentNullException(nameof(subSystemsService));
        }

        [HttpGet("sqlserverinfo")] // TODO: Add versioning to the route
        public async Task<IActionResult> GetSQLServerInfoAsync()
        {
            // TODO: Enforce explicit types 
            var info = await _subSystemsService.GetSQLServerInfoAsync();

            return Ok(new { info });
        }

        [HttpGet("licenciaspoc")]
        public async Task<IActionResult> GetLicenciasPoCAsync(string email, string password)
        {
            var licencias = await _subSystemsService.LoginAndGetLicenciasPoCAsync("https://montessori.bo/principal/public", email, password); // TODO: Move to static constants class or configuration

            return Content(licencias, "text/html");
        }
    }
}
