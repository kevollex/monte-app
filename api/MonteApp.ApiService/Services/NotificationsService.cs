using System;
using MonteApp.ApiService.Infrastructure;

namespace MonteApp.ApiService.Services;

public interface INotificationsService
{
    Task<int> SubscribeDeviceAsync(string sessionId, string deviceToken, string deviceType);
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(string sessionId);
}
public record NotificationDto(
        int NotificationId,
        string Title,
        string Body,
        string Status
    );
public class NotificationsService : INotificationsService
{
    private readonly IDatabase _db;

    public NotificationsService(IDatabase db) => _db = db;

    public async Task<int> SubscribeDeviceAsync(string sessionId, string deviceToken, string deviceType)
    {
        // 1) Inserta o actualiza el token en la tabla Devices
        var deviceId = await _db.UpsertDeviceAsync(deviceToken);

        // 2) (Opcional) Actualiza plataforma, etc:
        // await _db.UpdateDevicePlatformAsync(deviceId, deviceType);

        return deviceId;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(string sessionId)
    {
        // 1) Lee todos los pendientes
        var pend = await _db.GetPendingQueueAsync();

        var list = new List<NotificationDto>();
        foreach (var (queueId, notificationId, deviceId) in pend)
        {
            // 2) Lee título y cuerpo
            var (title, body) = await _db.GetNotificationContentAsync(notificationId);

            // 3) Marca luego como “Sent” o “Failed”
            //    await _db.UpdateQueueStatusAsync(queueId, "Sent", null);

            list.Add(new NotificationDto(
                notificationId,
                title,
                body,
                "Pending"
            ));
        }

        return list;
    }
}
