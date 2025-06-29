namespace MonteApp.ApiService.Services
{
    public interface INotificationsService
    {
        /// <summary>
        /// Encola una notificación de prueba en la base de datos.
        /// </summary>
        /// <param name="deviceId">Id del dispositivo destino.</param>
        /// <param name="title">Título de la notificación.</param>
        /// <param name="body">Cuerpo de la notificación.</param>
        /// <returns>El <c>NotificationId</c> generado.</returns>
        Task<int> EnqueueDemoAsync(int deviceId, string title, string body);

        /// <summary>
        /// Procesa todas las notificaciones pendientes en la cola:
        /// las envía vía Firebase y actualiza su estado en la BD.
        /// </summary>
        Task ProcessPendingAsync();
    }
}