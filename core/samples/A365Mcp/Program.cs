// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using A365Mcp;
using Microsoft.Teams.Apps;

// Wires up the Teams bot application and delegates AI execution to Agent.
// Handler registration lives in A365TeamsBotApp's constructor.

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddAgent(builder.Configuration);

WebApplication webApp = builder.Build();

webApp.UseTeamsBotApplication<A365TeamsBotApp>();

webApp.Run();
