// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CompatProactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Teams.Bot.Compat;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddCompatAdapter();
builder.Services.AddHostedService<ProactiveWorker>();
IHost host = builder.Build();
host.Run();
