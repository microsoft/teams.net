// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using A2A.AspNetCore;
using A2ABot;
using A2ABot.A2A;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using AgentCard = A2A.AgentCard;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddTeamsBotApplication();
builder.Services.AddHttpClient("a2a", c => c.Timeout = TimeSpan.FromSeconds(5));
builder.Services.AddSingleton<A2AClient>();
builder.Services.AddSingleton<Agent>();

Config config = new(
    Name:        builder.Configuration["Bot:Name"]        ?? throw new InvalidOperationException("Bot:Name is required."),
    SelfUrl:     builder.Configuration["Bot:SelfUrl"]     ?? throw new InvalidOperationException("Bot:SelfUrl is required."),
    Description: builder.Configuration["Bot:Description"] ?? throw new InvalidOperationException("Bot:Description is required."),
    PeerUrl:     builder.Configuration["Bot:PeerUrl"]     ?? throw new InvalidOperationException("Bot:PeerUrl is required."),
    PeerName:    builder.Configuration["Bot:PeerName"]    ?? throw new InvalidOperationException("Bot:PeerName is required."));

builder.Services.AddSingleton(config);

AgentCard agentCard = AgentCardFactory.Build(config);
builder.Services.AddA2AAgent<A2AServer>(agentCard);

WebApplication webApp = builder.Build();
Agent agent = webApp.Services.GetRequiredService<Agent>();
TeamsBotApplication teamsApp = webApp.UseTeamsBotApplication();

teamsApp.OnMessage(async (context, ct) =>
{
    string text        = context.Activity.Text?.Trim() ?? string.Empty;
    string convId      = context.Activity.Conversation!.Id!;
    string userName    = context.Activity.From?.Name ?? "User";
    string aadObjectId = context.Activity.From?.AadObjectId ?? string.Empty;
    string tenantId    = context.Activity.Conversation?.TenantId ?? string.Empty;
    string serviceUrl  = context.Activity.ServiceUrl?.ToString() ?? string.Empty;

    string reply = await agent.RunAsync(convId, aadObjectId, userName, tenantId, serviceUrl, text, ct);
    if (!string.IsNullOrWhiteSpace(reply))
        await context.SendActivityAsync(reply, ct);
});

webApp.MapA2A("/a2a");
webApp.MapWellKnownAgentCard(agentCard);

webApp.Run();
