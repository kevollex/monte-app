using MonteApp.NotificationWorker;
using MonteApp.NotificationWorker.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

builder.AddKeyedSqlServerClient( name: "monteappdb" );
builder.Services.AddScoped<IDatabase, Database>();

builder.AddKeyedSqlServerClient( name: "montessoribodb" );
builder.Services.AddScoped<IMontessoriBoDatabase, MontessoriBoDatabase>();

var host = builder.Build();
host.Run();
