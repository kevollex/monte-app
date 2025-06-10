var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sql = builder.AddSqlServer("sql")
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithDataVolume("monteappdb-datavolume");

var db = sql.AddDatabase("monteappdb");

var apiService = builder.AddProject<Projects.MonteApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.MonteApp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddNpmApp("pwavite", "../../pwa")
    .WithExternalHttpEndpoints()
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .PublishAsDockerFile();

builder.AddDockerComposeEnvironment("compose");

builder.Build().Run();
