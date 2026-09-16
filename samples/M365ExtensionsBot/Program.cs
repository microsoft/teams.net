// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;
using Microsoft.Teams.M365Extensions;
using M365ExtensionsBot;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Agents SDK ─────────────────────────────────────────────────────
builder.AddAgent<MyAgent>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.Services.AddSingleton<ConversationState>();
builder.Services.AddAgentAspNetAuthentication(builder.Configuration);

// ── Teams SDK ──────────────────────────────────────────────────────
// One call: registers MyTeamsBot + its Teams API/auth chain (via AgentSdkAuthHandler)
// and installs the routing middleware on the CloudAdapter pipeline. This sample
// keeps signin/* invokes on the Agents SDK side by bypassing Teams routing for them.
builder.Services.AddTeamsSdk<MyTeamsBot>(shouldBypassTeams: turnContext =>
    turnContext.Activity.Type == ActivityTypes.Invoke
    && !string.IsNullOrEmpty(turnContext.Activity.Name)
    && turnContext.Activity.Name.StartsWith("signin/", StringComparison.OrdinalIgnoreCase));

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapAgentRootEndpoint();
app.MapAgentApplicationEndpoints(requireAuth: !app.Environment.IsDevelopment());

app.Run();
