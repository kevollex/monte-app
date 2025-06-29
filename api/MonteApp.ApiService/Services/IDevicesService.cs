namespace MonteApp.ApiService.Services
{
    public interface IDevicesService
    {
        Task<int>    UpsertDeviceAsync(string fcmToken);
        Task         DeleteDeviceAsync(int deviceId);
    }
}