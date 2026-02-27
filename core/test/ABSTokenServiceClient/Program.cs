// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABSTokenServiceClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Teams.Bot.Core;
using Microsoft.Teams.Bot.Core.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddUserTokenClient();
builder.Services.AddHostedService<UserTokenCLIService>();
WebApplication host = builder.Build();
host.Run();
