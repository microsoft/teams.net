// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Bot.Core.Hosting;

using Proactive;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddConversationClient();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
