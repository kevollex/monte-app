using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonteApp.ApiService.Services;

namespace MonteApp.ApiService.Controllers
{
    // [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationsService _notificationsService;

        public NotificationsController(INotificationsService notificationsService)
        {
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var notifications = await _notificationsService.GetNotificationsAsync(jti);
            if (notifications == null || !notifications.Any())
            {
                return NoContent(); // Return 204 No Content if there are no notifications
            }

            return Ok(notifications); // Return 200 OK with the notifications
        }

        [HttpPost("subscribe-device")]
        public async Task<IActionResult> SubscribeDevice([FromBody] DeviceSubscriptionRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.DeviceToken) || string.IsNullOrEmpty(request.DeviceType))
            {
                return BadRequest("Invalid device subscription request.");
            }

            var jti = User.FindFirst("jti")?.Value ?? throw new UnauthorizedAccessException("jti value on JWT token not found.");
            var result = await _notificationsService.SubscribeDeviceAsync(jti, request.DeviceToken, request.DeviceType);

            return Ok(result); // Return 200 OK with the subscription result
        }

        public record DeviceSubscriptionRequest(string DeviceToken, string DeviceType);
    }
}
