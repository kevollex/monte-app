using MonteApp.NotificationWorker.Infrastructure;

namespace MonteApp.NotificationWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private int _executionCount;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running.");

        // When the timer should have no due-time, then do the work once now.
        // await DoWork(database, montessoriBoDatabase);

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
                var montessoriBoDatabase = scope.ServiceProvider.GetRequiredService<IMontessoriBoDatabase>();

                await DoWork(database, montessoriBoDatabase);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker is stopping.");
        }
    }

    private async Task DoWork(IDatabase database, IMontessoriBoDatabase montessoriBoDatabase)
    {
        int count = Interlocked.Increment(ref _executionCount);
        string sqlMessage = await database.GetSQLServerInfoAsync();

        // Simulate work
        await Task.Delay(TimeSpan.FromSeconds(2));

        _logger.LogInformation("Worker is working. Count: {Count}", count);
        _logger.LogInformation("Worker is working. SqlServerMessage: {Message}", sqlMessage);
    }
}
