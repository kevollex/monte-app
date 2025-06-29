var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var databaseName = "monteappdb"; // TODO: Grab value from config or use default
var sqlPassword = builder.AddParameter("sqlpassword", true);
int sqlPort = 1434; // Grab value from config or use default
var sql = builder.AddSqlServer("sql", sqlPassword, port: sqlPort)
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithDataVolume($"{databaseName}-datavolume");

var creationScript = $$"""
    -- Create database if it doesn't exist
    IF DB_ID('{{databaseName}}') IS NULL
        CREATE DATABASE [{{databaseName}}];
    GO

    USE [{{databaseName}}];
    GO

    -- Users table
    CREATE TABLE [users] (
        id INT PRIMARY KEY IDENTITY(1,1),
        email VARCHAR(255) NOT NULL UNIQUE,
        password_hash VARCHAR(255) NOT NULL,
        full_name VARCHAR(255),
        is_active BIT DEFAULT 1,
        created_at DATETIME DEFAULT GETDATE()
    );
    GO

    -- Sessions table (for session information management)
    CREATE TABLE [sessions] (
        id INT PRIMARY KEY IDENTITY(1,1),
        user_id INT NOT NULL,
        csrf_token VARCHAR(512) NOT NULL,
        jwt_id VARCHAR(128) NOT NULL,
        created_at DATETIME DEFAULT GETDATE(),
        expires_at DATETIME NULL,
        revoked_at DATETIME NULL,
        cookies NVARCHAR(MAX) NULL, -- Store cookies as a string
        FOREIGN KEY (user_id) REFERENCES [users](id) ON DELETE CASCADE
    );
    GO
    """;

var db = sql.AddDatabase(databaseName)
            .WithCreationScript(creationScript);

var apiService = builder.AddProject<Projects.MonteApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);

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
