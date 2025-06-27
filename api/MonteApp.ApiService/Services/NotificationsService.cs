using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using MonteApp.ApiService.Infrastructure;

namespace MonteApp.ApiService.Services
{
    public class NotificationsService : INotificationsService
    {
        private readonly IDatabase _database;
    
        private readonly FirebaseMessaging _fcm;
        //private readonly ILogger<NotificationsService> _logger;
        
        public NotificationsService(
            IDatabase db,
            IConfiguration config) {
            this._database = db;


            var credPath = config["Firebase:ServiceAccountFile"]
                           ?? throw new InvalidOperationException("Missing Firebase:ServiceAccountFile in configuration");

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


            int notificationId = await _database.InsertNotificationAsync(title, body);
            await _database.InsertNotificationQueueAsync(notificationId, deviceId);
            return notificationId;
        }

        public async Task ProcessPendingAsync()
        {
            // await EnsureConnectionOpenAsync();
            // var items = new List<(int QueueId, int NotificationId, int DeviceId)>();

            // using (var cmdRead = new SqlCommand(
            //     "SELECT QueueId, NotificationId, DeviceId FROM NotificationQueue WHERE Status = 'Pending'",
            //     _conn))
            // using (var reader = await cmdRead.ExecuteReaderAsync())
            // {
            //     while (await reader.ReadAsync())
            //         items.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
            // }

            // foreach (var (queueId, nid, did) in items)
            //     await ProcessSingleAsync(queueId, nid, did);
        }

        private async Task ProcessSingleAsync(int queueId, int nid, int did)
        {
        //     string status = "Sent";
        //     string? err = null;

        //     try
        //     {
        //         // 1) Obtiene título y cuerpo
        //         var (title, body) = await GetNotificationContentAsync(nid);

        //         // 2) Obtiene token del dispositivo
        //         var token = await GetDeviceTokenAsync(did);

        //         // 3) Envía push
        //         var message = new Message
        //         {
        //             Token = token,
        //             Notification = new Notification { Title = title, Body = body }
        //         };
        //         await _fcm.SendAsync(message);
        //         //_logger.LogInformation("Sent notification {QueueId} to device {DeviceId}", queueId, did);
        //     }
        //     catch (Exception ex)
        //     {
        //         status = "Failed";
        //         err = ex.Message;
        //         //_logger.LogError(ex, "Failed to send notification {QueueId}", queueId);
        //     }

        //     // 4) Actualiza estado en la cola
        //     using var cmdUpd = new SqlCommand(
        //         @"UPDATE NotificationQueue
        //   SET Status=@s, SentAt=GETDATE(), ErrorMessage=@e
        //   WHERE QueueId=@qid",
        //         _conn);
        //     cmdUpd.Parameters.AddWithValue("@s", status);
        //     cmdUpd.Parameters.AddWithValue("@e", (object?)err ?? DBNull.Value);
        //     cmdUpd.Parameters.AddWithValue("@qid", queueId);
        //     await cmdUpd.ExecuteNonQueryAsync();
        }

        // private async Task<(string Title, string Body)> GetNotificationContentAsync(int nid)
        // {
        // //     using var cmd = new SqlCommand(
        // //         "SELECT Title, Body FROM Notifications WHERE NotificationId = @nid",
        // //         _conn);
        // //     cmd.Parameters.AddWithValue("@nid", nid);

        // //     using var reader = await cmd.ExecuteReaderAsync();
        // //     if (!await reader.ReadAsync())
        // //         throw new Exception($"Notification {nid} not found.");

        // //     return (reader.GetString(0), reader.GetString(1));
        // // }

        // // /// <summary>
        // // /// Recupera el token FCM guardado para un dispositivo concreto.
        // // /// </summary>
        // // private async Task<string> GetDeviceTokenAsync(int did)
        // // {
        // //     using var cmd = new SqlCommand(
        // //         "SELECT TokenFCM FROM Devices WHERE DeviceId = @did",
        // //         _conn);
        // //     cmd.Parameters.AddWithValue("@did", did);

        // //     var tokenObj = await cmd.ExecuteScalarAsync();
        // //     if (tokenObj == null || tokenObj == DBNull.Value)
        // //         throw new Exception($"Device token for {did} not found.");

        // //     return (string)tokenObj;
        // }

        /// <summary>
        /// Asegura que la conexión SQL está abierta.
        /// </summary>
        private async Task EnsureConnectionOpenAsync()
        {
            // if (_conn.State != ConnectionState.Open)
            //     await _conn.OpenAsync();
        }       // … resto de métodos privados (ProcessSingleAsync, GetNotificationContentAsync, etc.) …

    }
}
