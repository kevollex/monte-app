using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using MonteApp.NotificationWorker;
using MonteApp.NotificationWorker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

var firebaseApp = FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("service-account.json")
});

var messaging = FirebaseMessaging.GetMessaging(firebaseApp);
builder.Services.AddSingleton<FirebaseMessaging>(messaging);

builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton(FirebaseMessaging.DefaultInstance);

builder.AddKeyedSqlServerClient( name: "monteappdb" );
builder.Services.AddScoped<IDatabase, Database>();

builder.AddKeyedSqlServerClient( name: "montessoribodb" );
builder.Services.AddScoped<IMontessoriBoDatabase, MontessoriBoDatabase>();

var host = builder.Build();
host.Run();
