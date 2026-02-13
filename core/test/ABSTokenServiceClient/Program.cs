// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABSTokenServiceClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserTokenClient();
builder.Services.AddHostedService<UserTokenCLIService>();
WebApplication host = builder.Build();
host.Run();
