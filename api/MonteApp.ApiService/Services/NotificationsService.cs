using System;
using MonteApp.ApiService.Infrastructure;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace MonteApp.ApiService.Services;

public class NotificationsService: INotificationsService {
    private readonly IDatabase _database;
    private readonly FirebaseMessaging _fcm;
    public NotificationsService(IDatabase db, IConfiguration config)
    {
        this._database = db;
        var credPath = config["Firebase:ServiceAccountFile"]
            ?? throw new InvalidOperationException("Falta Firebase:ServiceAccountFile en appsettings.json");
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(credPath)
            });
        }
        _fcm = FirebaseMessaging.GetMessaging(FirebaseApp.DefaultInstance!);
    }

public async Task<int> EnqueueDemoAsync(int deviceId, string title, string body)
{
    var id = await _database.InsertNotificationAsync(title, body);
    await _database.InsertNotificationQueueAsync(id, deviceId);
    return id;
}

    public async Task ProcessPendingAsync()
{
    var pendientes = await _database.GetPendingQueueAsync();

    foreach (var (queueId, nid, did) in pendientes)
    {
        string status = "Sent";
        string? error  = null;

        try
        {
            var (title, body) = await _database.GetNotificationContentAsync(nid);
            var token         = await _database.GetDeviceTokenAsync(did);

            var message = new Message {
                Token        = token,
                Notification = new Notification { Title = title, Body = body }
            };
            await _fcm.SendAsync(message);
        }
        catch (Exception ex)
        {
            status = "Failed";
            error  = ex.Message;
        }

        await _database.UpdateQueueStatusAsync(queueId, status, error);
    }
}
}