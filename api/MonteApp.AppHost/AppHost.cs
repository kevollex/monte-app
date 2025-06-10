var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sqlPassword = builder.AddParameter("sqlpassword", true);
var sql = builder.AddSqlServer("sql", sqlPassword)
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithDataVolume("monteappdb-datavolume");

var databaseName = "monteappdb"; // TODO: Grab name from config
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

    -- Roles table
    CREATE TABLE [roles] (
        id INT PRIMARY KEY IDENTITY(1,1),
        name VARCHAR(100) NOT NULL UNIQUE
    );
    GO

    -- UserRoles table (many-to-many)
    CREATE TABLE [user_roles] (
        user_id INT NOT NULL,
        role_id INT NOT NULL,
        PRIMARY KEY (user_id, role_id),
        FOREIGN KEY (user_id) REFERENCES [users](id) ON DELETE CASCADE,
        FOREIGN KEY (role_id) REFERENCES [roles](id) ON DELETE CASCADE
    );
    GO

    -- RefreshTokens table (for JWT refresh token management)
    CREATE TABLE [refresh_tokens] (
        id INT PRIMARY KEY IDENTITY(1,1),
        user_id INT NOT NULL,
        token VARCHAR(512) NOT NULL,
        expires_at DATETIME NOT NULL,
        created_at DATETIME DEFAULT GETDATE(),
        revoked_at DATETIME NULL,
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
