// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CustomHosting;
using Microsoft.Teams.Bot.Apps;
using Microsoft.Teams.Bot.Core.Hosting;

WebApplicationBuilder webAppBuilder = WebApplication.CreateSlimBuilder(args);
webAppBuilder.Services.AddTeamsBotApplication<MyTeamsBotApp>();
WebApplication webApp = webAppBuilder.Build();

webApp.MapGet("/", () => $"Teams Bot App is running {TeamsBotApplication.Version}.");
webApp.UseBotApplication<MyTeamsBotApp>();

webApp.Run();
