using CompatProactive;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Bot.Compat;
using Microsoft.Teams.Bot.Core.Hosting;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCompatAdapter();
builder.Services.AddHostedService<ProactiveWorker>();
IHost host = builder.Build();
host.Run();
