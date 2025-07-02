using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Models;
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

        [HttpGet("control-semanal")]
        [Authorize]
        public async Task<IActionResult> GetControlSemanalPage()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var controlSemanal = await _montessoriBoWrapperService.GetPageAsync(jti, Constants.SubsysControlSemanalUrl);
         
            return Content(controlSemanal, "text/html");
        }

        [HttpGet("cartas-recibidas")]
        [Authorize]
        public async Task<IActionResult> GetCartasRecibidasPage()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var controlSemanal = await _montessoriBoWrapperService.GetPageAsync(jti, Constants.SubsysCartasRecibidasUrl);
         
            return Content(controlSemanal, "text/html");
        }

        [HttpGet("circulares")]
        [Authorize]
        public async Task<IActionResult> GetCircularesPage()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var controlSemanal = await _montessoriBoWrapperService.GetPageAsync(jti, Constants.SubsysCircularesUrl);
         
            return Content(controlSemanal, "text/html");
        }

        [HttpGet("licencias")]
        [Authorize]
        public async Task<IActionResult> GetLicenciasPage()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var licencias = await _montessoriBoWrapperService.GetLicenciasPageAsync(jti, true); // TODO: Remove the bypass parameter

            return Content(licencias, "text/html");
        }

        [HttpPost("licencias/licencias-alumnos")]
        public async Task<IActionResult> GetLicenciasAlumnosData(string id, string sessionId)
        {
            // How to authorize this proxy endpoint?
            // var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var licenciasAlumnos = await _montessoriBoWrapperService.GetLicenciasAlumnosAsync(id, sessionId);

            return Content(licenciasAlumnos, "text/html");
        }

        [HttpPost("licencias/licencia-envia")]
        public async Task<IActionResult> PostLicenciaEnvia(
            [FromForm] string idalumno,
            [FromForm] string nombrehijo,
            [FromForm] string motivo,
            [FromForm] string fechadesde,
            [FromForm] string fechahasta,
            string sessionId)
        {
            // TODO: Process the data via the MontessoriBoWrapperService
            return new JsonResult(new { tipoRespuesta = 1 });
        }

    }
}
