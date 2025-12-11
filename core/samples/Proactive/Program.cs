using Microsoft.Bot.Core.Hosting;

using Proactive;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddBotApplicationClients();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
