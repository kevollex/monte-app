using MonteApp.ApiService.Infrastructure;

namespace MonteApp.ApiService.Services
{
    public class DevicesService : IDevicesService
    {
        private readonly IDatabase _db;

        public DevicesService(IDatabase db)
            => _db = db;

        public Task<int> UpsertDeviceAsync(string fcmToken)
            => _db.UpsertDeviceAsync(fcmToken);

        public Task DeleteDeviceAsync(int deviceId)
            => _db.DeleteDeviceAsync(deviceId);
    }
}