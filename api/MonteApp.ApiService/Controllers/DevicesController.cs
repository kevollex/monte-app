using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    [ApiController]
    [Route("devices")]
    public class DevicesController : ControllerBase
    {
        private readonly IDevicesService _svc;

        public DevicesController(IDevicesService svc)
            => _svc = svc;


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var deviceId = await _svc.UpsertDeviceAsync(dto.FcmToken);
            return Ok(new { deviceId });
        }

        [HttpDelete("{deviceId:int}")]
        public async Task<IActionResult> Delete(int deviceId)
        {
            await _svc.DeleteDeviceAsync(deviceId);
            return NoContent();
        }

        public record RegisterDto(string FcmToken);
    }
}