// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using McpServer;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddTeamsBotApplication();
// State is a singleton so the same maps are shared between the bot's
// activity handlers and the MCP tools. Replace with a persistent store for production.
builder.Services.AddSingleton<State>();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<McpTools>();

WebApplication webApp = builder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();
State state = webApp.Services.GetRequiredService<State>();

bot.OnMessage(async (context, cancellationToken) =>
{
    string userId = context.Activity.From?.Id ?? string.Empty;
    string conversationId = context.Activity.Conversation?.Id ?? string.Empty;

    // Cache the service URL: proactive sends and conversations.create both
    // require one, and there's no way to discover it without an inbound activity.
    if (context.Activity.ServiceUrl is not null)
    {
        state.LastServiceUrl = context.Activity.ServiceUrl;
    }

    // cache the personal conversation_id so MCP tools can DM this user later.
    TeamsConversation? conv = TeamsConversation.FromConversation(context.Activity.Conversation);
    if (conv?.ConversationType == ConversationType.Personal && !string.IsNullOrEmpty(userId))
    {
        state.Conversations[userId] = conversationId;
    }

    // If this user has a pending ask, treat their next message as the answer.
    // Only one outstanding ask per user is supported (see README Limitations).
    if (state.UserPendingAsk.TryRemove(userId, out string? requestId)
        && state.PendingAsks.TryGetValue(requestId, out PendingAsk? entry))
    {
        entry.Reply = context.Activity.Text ?? string.Empty;
        entry.Status = AskStatus.Answered;
        await context.Send("Got it, thank you!", cancellationToken);
        return;
    }

    Console.WriteLine(
        $"Received message from user {userId} in conversation {conversationId}, but no pending ask found.");
    await context.Send("Hi! I'll let you know if I need anything.", cancellationToken);
});


bot.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    AdaptiveCardAction? action = context.Activity.Value?.Action;
    if (action?.Verb != "approval_response")
    {
        return AdaptiveCardResponse.CreateMessageResponse("Unknown action");
    }

    string? approvalId = TryGetString(action.Data, "approval_id");
    string? decision = TryGetString(action.Data, "decision");

    if (approvalId is not null
        && state.Approvals.ContainsKey(approvalId)
        && (decision == ApprovalStatus.Approved || decision == ApprovalStatus.Rejected))
    {
        state.Approvals[approvalId] = decision;
    }

    await Task.CompletedTask;
    return AdaptiveCardResponse.CreateMessageResponse("Response recorded");
});

webApp.MapMcp("/mcp");
webApp.Run();

static string? TryGetString(Dictionary<string, object>? data, string key)
    => data is not null && data.TryGetValue(key, out object? value) ? value?.ToString() : null;
