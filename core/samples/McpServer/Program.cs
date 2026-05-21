// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using McpServer;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Handlers;
using Microsoft.Teams.Apps.Schema;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddTeamsBotApplication();
// State is a singleton so the same maps are shared between the bot's
// activity handlers and the MCP tools. Replace with a persistent store for production.
builder.Services.AddSingleton<State>();
builder.Services.AddHttpClient<GraphClient>();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<McpTools>();

WebApplication webApp = builder.Build();

TeamsBotApplication bot = webApp.UseTeamsBotApplication();
State state = webApp.Services.GetRequiredService<State>();
ILogger<Program> logger = webApp.Services.GetRequiredService<ILogger<Program>>();


bot.OnMessage(async (context, cancellationToken) =>
{
    string userId = context.Activity.From?.AadObjectId ?? string.Empty;
    string conversationId = context.Activity.Conversation?.Id ?? string.Empty;

    if (context.Activity.ServiceUrl is not null)
        state.ServiceUrl = context.Activity.ServiceUrl;

    // cache the personal conversation_id so MCP tools can DM this user later.
    TeamsConversation? conv = TeamsConversation.FromConversation(context.Activity.Conversation);
    if (conv?.ConversationType == ConversationType.Personal && !string.IsNullOrEmpty(userId))
    {
        state.Conversations[userId] = conversationId;
    }

    // If this user has a pending ask, treat their next message as the answer.
    // Only one outstanding ask per user is supported (see README Limitations).
    if (!string.IsNullOrEmpty(userId)
        && state.UserPendingAsk.TryRemove(userId, out string? requestId)
        && state.PendingAsks.TryGetValue(requestId, out PendingAsk? entry))
    {
        PendingAsk answered = entry with { Status = AskStatus.Answered, Reply = context.Activity.Text ?? string.Empty };
        state.PendingAsks.TryUpdate(requestId, answered, entry);
        await context.SendActivityAsync("Got it, thank you!", cancellationToken);
        return;
    }

    logger.LogInformation(
        "Received message from user {UserId} in conversation {ConversationId}, but no pending ask found.",
        userId, conversationId);
    await context.SendActivityAsync("Hi! I'll let you know if I need anything.", cancellationToken);
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
        && (decision == ApprovalStatus.Approved || decision == ApprovalStatus.Rejected)
        && state.Approvals.TryGetValue(approvalId, out string? currentDecision)
        && state.Approvals.TryUpdate(approvalId, decision, currentDecision))
    {
        return AdaptiveCardResponse.CreateMessageResponse("Response recorded");
    }

    await Task.CompletedTask;
    return AdaptiveCardResponse.CreateMessageResponse(
        "Unable to record response. The approval request may be invalid or expired.");
});

webApp.MapMcp("/mcp");
webApp.Run();

static string? TryGetString(Dictionary<string, object>? data, string key)
    => data is not null && data.TryGetValue(key, out object? value) ? value?.ToString() : null;
