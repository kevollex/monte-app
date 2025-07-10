using System.Data.Common;
using System.Runtime.InteropServices;
using FirebaseAdmin.Messaging;
using MonteApp.NotificationWorker.Infrastructure;

namespace MonteApp.NotificationWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, FirebaseMessaging fcm)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _fcm = fcm;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running.");

        // When the timer should have no due-time, then do the work once now.
        // await DoWork(database, montessoriBoDatabase);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
                var montessoriBoDatabase = scope.ServiceProvider.GetRequiredService<IMontessoriBoDatabase>();

                await ProcessPendingAsync(database, montessoriBoDatabase);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker is stopping.");
        }
    }

    private readonly FirebaseMessaging _fcm;

private async Task ProcessPendingAsync(IDatabase db, IMontessoriBoDatabase montessoriBoDatabase)
{
    var mensajes = await montessoriBoDatabase.GetPendingMensajesAsync();
    foreach (var m in mensajes)
    {
        // 1) Creamos notificación en MonteApp
        var nid = await db.InsertNotificationAsync(m.Asignacion, m.Texto);

        // 2) La encolamos para el device (usa el deviceId que tengas, p.ej. el tuyo)
        var deviceId = 4;
        await db.InsertNotificationQueueAsync(nid, deviceId);

        // 3) Marcamos ese mensaje ya procesado en MontessoriBo
        await montessoriBoDatabase.MarkMensajeProcessedAsync(m.IdMensaje);
    }

    var pendientes = await db.GetPendingQueueAsync();
            foreach (var (queueId, notificationId, deviceId) in pendientes)
            {
                try
                {
                    // 1) Token FCM
                    var token = await db.GetDeviceTokenAsync(deviceId);

                    // 2) Contenido
                    var (title, body) = await db.GetNotificationContentAsync(notificationId);

                    // 3) Construye mensaje
                    var message = new Message
                    {
                        Token        = token,
                        Notification = new Notification { Title = title, Body = body }
                    };

                    // 4) Envía a FCM
                    await _fcm.SendAsync(message);

                    // 5) Marca como enviado
                    await db.UpdateQueueStatusAsync(queueId, "sent", null);
                    _logger.LogInformation("Sent notification {QueueId}", queueId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending notification {QueueId}", queueId);
                    await db.UpdateQueueStatusAsync(queueId, "failed", ex.Message);
                }
            }
        }
    }

