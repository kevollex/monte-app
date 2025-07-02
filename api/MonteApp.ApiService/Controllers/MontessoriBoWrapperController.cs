using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    // [Route("api/[controller]")] TODO: Better routing
    [Route("proxy")]
    [ApiController]
    // [Authorize]
    public class MontessoriBoWrapperController : ControllerBase
    {
        private readonly IMontessoriBoWrapperService _montessoriBoWrapperService;

        public MontessoriBoWrapperController(IMontessoriBoWrapperService montessoriBoWrapperService)
        {
            _montessoriBoWrapperService = montessoriBoWrapperService ?? throw new ArgumentNullException(nameof(montessoriBoWrapperService));
        }

        [HttpGet("home")]
        [Authorize]
        public async Task<IActionResult> GetHomeData()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            return Ok(await _montessoriBoWrapperService.GetHomeDataAsync(jti));
        }

        [HttpGet("licencias")]
        [Authorize]
        public async Task<IActionResult> GetLicenciasPage()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var licencias = await _montessoriBoWrapperService.GetLicenciasPageAsync(jti);

            return Content(licencias, "text/html");
        }

        [HttpPost("licencias-alumnos")]
        public async Task<IActionResult> GetLicenciasAlumnosData(string id, string sessionId)
        {
            // How to authorize this proxy endpoint?
            // var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var licenciasAlumnos = await _montessoriBoWrapperService.GetLicenciasAlumnosAsync(id, sessionId);

            return Content(licenciasAlumnos, "text/html");
        }

    }
}
