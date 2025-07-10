using System;
using Microsoft.Data.SqlClient;

namespace MonteApp.NotificationWorker.Infrastructure;

public interface IMontessoriBoDatabase
{
    Task<List<MensajeBo>> GetPendingMensajesAsync();
    Task MarkMensajeProcessedAsync(int idMensaje);
}
public record MensajeBo(
    int IdMensaje,
    DateTime FechaRegistro,
    int Estado,
    int IdPersonal,
    string Asignacion,
    string Texto
);


public class MontessoriBoDatabase : IMontessoriBoDatabase
{
    private readonly SqlConnection _connection;

    public MontessoriBoDatabase([FromKeyedServices("montessoribodb")] SqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    public async Task<List<MensajeBo>> GetPendingMensajesAsync()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        var list = new List<MensajeBo>();
        using var cmd = new SqlCommand(@"
            SELECT id_mensaje, fecha_registro, estado, idpersonal, asignacion, texto
            FROM mensajes
            WHERE estado = 0", _connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new MensajeBo(
                reader.GetInt32(0),                // id_mensaje
                reader.GetDateTime(1),               // fecha_registro
                reader.GetInt32(2),                  // estado
                reader.GetInt32(3),                  // idpersonal
                reader.GetString(4),                 // asignacion
                reader.GetString(5)                  // texto
            ));
        }
        return list;
    }

    public async Task MarkMensajeProcessedAsync(int idMensaje)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        using var cmd = new SqlCommand(@"
            UPDATE mensajes
            SET estado = 1
            WHERE id_mensaje = @id", _connection);
        cmd.Parameters.AddWithValue("@id", idMensaje);
        await cmd.ExecuteNonQueryAsync();
    }

}
