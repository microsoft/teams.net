// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using A365Mcp;
using Microsoft.Teams.Apps;

// Wires up the Teams bot application and delegates AI execution to Agent.
// Handler registration lives in TeamsBotAppHandlers.cs.

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();
builder.Services.AddAgent(builder.Configuration);

WebApplication webApp = builder.Build();

webApp.UseTeamsBotApplication().RegisterHandlers(webApp.Services);

webApp.Run();
