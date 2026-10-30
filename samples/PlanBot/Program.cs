// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using PlanBot;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();
WebApplication webApp = builder.Build();
TeamsBotApplication teams = webApp.UseTeamsBotApplication();
PlanDemo demo = new(webApp.Logger, webApp.Lifetime.ApplicationStopping);
teams.OnMessage(demo.HandleAsync);

await webApp.RunAsync();
await demo.WaitForPendingAsync();
