using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    [ApiController]
    [Route("notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationsService _svc;

        public NotificationsController(INotificationsService svc)
            => _svc = svc;

        [HttpPost("demo")]
        public async Task<IActionResult> Demo([FromBody] DemoDto dto)
        {
            var id = await _svc.EnqueueDemoAsync(dto.DeviceId, dto.Title, dto.Body);
            return Ok(new { notificationId = id });
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send()
        {
            await _svc.ProcessPendingAsync();
            return Ok(new { status = "Processed" });
        }
    }

    public record DemoDto(int DeviceId, string Title, string Body);
}