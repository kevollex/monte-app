using System;
using Microsoft.Data.SqlClient;

namespace MonteApp.NotificationWorker.Infrastructure;

public interface IMontessoriBoDatabase
{
    
}

public class MontessoriBoDatabase : IMontessoriBoDatabase
{
    private readonly SqlConnection _connection;

    public MontessoriBoDatabase([FromKeyedServices("montessoribodb")] SqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

}
