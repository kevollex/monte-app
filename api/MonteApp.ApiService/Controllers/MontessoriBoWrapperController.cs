using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    // [Route("api/[controller]")] TODO: Better routing
    [ApiController]
    [Authorize]
    public class MontessoriBoWrapperController : ControllerBase
    {
        private readonly IMontessoriBoWrapperService _montessoriBoWrapperService;

        public MontessoriBoWrapperController(IMontessoriBoWrapperService montessoriBoWrapperService)
        {
            _montessoriBoWrapperService = montessoriBoWrapperService ?? throw new ArgumentNullException(nameof(montessoriBoWrapperService));
        }

        [HttpGet("home")]
        public async Task<IActionResult> GetHomeData()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            return Ok(await _montessoriBoWrapperService.GetHomeDataAsync(jti));
        }

        [HttpGet("licencias")]
        public async Task<IActionResult> GetLicenciasPage()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var licencias = await _montessoriBoWrapperService.GetLicenciasPageAsync(jti);

            return Content(licencias, "text/html");
        }

    }
}
