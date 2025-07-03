using System;

namespace MonteApp.ApiService.Services;

public interface INotificationsService
{
    Task<string> SubscribeDeviceAsync(string sessionId, string deviceToken, string deviceType);
    Task<IEnumerable<Object>> GetNotificationsAsync(string sessionId);
}

public class NotificationsService : INotificationsService
{
    public Task<IEnumerable<object>> GetNotificationsAsync(string sessionId)
    {
        throw new NotImplementedException();
    }

    public Task<string> SubscribeDeviceAsync(string sessionId, string deviceToken, string deviceType)
    {
        throw new NotImplementedException();
    }
}
