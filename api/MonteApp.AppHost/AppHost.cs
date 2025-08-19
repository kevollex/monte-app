var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var sqlPassword = builder.AddParameter("sqlpassword", true);
int sqlPort = 1434; // Grab value from config or use default

var montessoriboDatabaseName = "montessoribodb";
var databaseName = "monteappdb";

var sql = builder.AddSqlServer("sql", sqlPassword, port: sqlPort)
                 .WithLifetime(ContainerLifetime.Persistent)
                 .WithDataVolume($"{databaseName}-datavolume"); // TODO: General name for data volume

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
    full_name VARCHAR(255) NULL,
    is_active BIT DEFAULT 1,
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- Sessions table
CREATE TABLE [sessions] (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL,
    csrf_token VARCHAR(512) NOT NULL,
    jwt_id VARCHAR(128) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    expires_at DATETIME NULL,
    revoked_at DATETIME NULL,
    cookies NVARCHAR(MAX) NULL,
    updated_at DATETIME NULL,
    FOREIGN KEY (user_id) REFERENCES [users](id) ON DELETE CASCADE
);
GO

-- Devices table
CREATE TABLE [devices] (
    device_id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL,
    token_fcm NVARCHAR(1024) NULL,
    platform NVARCHAR(100) NULL,
    is_active BIT DEFAULT 1,
    registered_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (user_id) REFERENCES [users](id) ON DELETE CASCADE
);
GO

-- Notifications table
CREATE TABLE [notifications] (
    notification_id INT PRIMARY KEY IDENTITY(1,1),
    created_by_user_id INT NOT NULL,
    type NVARCHAR(100) NULL,
    title NVARCHAR(400) NULL,
    body NVARCHAR(MAX) NULL,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (created_by_user_id) REFERENCES [users](id)
);
GO

-- Notification queue table
CREATE TABLE [notification_queue] (
    queue_id INT PRIMARY KEY IDENTITY(1,1),
    notification_id INT NOT NULL,
    device_id INT NOT NULL,
    status NVARCHAR(40) NULL,
    scheduled_at DATETIME DEFAULT GETDATE(),
    sent_at DATETIME NULL,
    error_message NVARCHAR(1000) NULL,
    FOREIGN KEY (notification_id) REFERENCES [notifications](notification_id),
    FOREIGN KEY (device_id)       REFERENCES [devices](device_id)
);
GO
""";

var monteAppDb = sql.AddDatabase(databaseName)
            .WithCreationScript(creationScript);

var montessoriBoDb = sql.AddDatabase(montessoriboDatabaseName)
    .WithCreationScript($$"""
        -- Create database if it doesn't exist
        IF DB_ID('montessoribodb') IS NULL
            CREATE DATABASE [montessoribodb];
        GO

        USE [montessoribodb];
        GO

        -- Mensajes table
        CREATE TABLE [mensajes] (
            id_mensaje     INT           IDENTITY(1,1) PRIMARY KEY,
            fecha_registro DATETIME      NOT NULL,
            estado         INT           NOT NULL,
            idpersonal     INT           NOT NULL,
            asignacion     NVARCHAR(200) NOT NULL,
            texto          NVARCHAR(MAX) NOT NULL
        );
        GO
    """);
builder.AddProject<Projects.MonteApp_NotificationWorker>("notificationworker")
    .WithReference(montessoriBoDb)
    .WaitFor(montessoriBoDb)
    .WithReference(monteAppDb)
    .WaitFor(monteAppDb);

var apiService = builder.AddProject<Projects.MonteApp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(monteAppDb)
    .WaitFor(monteAppDb);

builder.AddNpmApp("pwavite", "../../pwa")
    .WithExternalHttpEndpoints()
    .WithEnvironment("BROWSER", "none")
    .WithHttpsEndpoint(port: 59053, env: "VITE_PORT")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .PublishAsDockerFile();

builder.AddDockerComposeEnvironment("compose");

builder.Build().Run();
