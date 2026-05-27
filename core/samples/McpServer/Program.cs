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

    logger.LogInformation(
        "Received message from user {UserId} in conversation {ConversationId}. Replies to asks now arrive via adaptive card actions.",
        userId, conversationId);
    await context.SendActivityAsync("Hi! I'll let you know if I need anything.", cancellationToken);
});


bot.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    AdaptiveCardAction? action = context.Activity.Value?.Action;

    switch (action?.Verb)
    {
        case "approval_response":
            return HandleApprovalResponse(action, state);
        case "ask_reply":
            return HandleAskReply(action, state);
        default:
            await Task.CompletedTask;
            return AdaptiveCardResponse.CreateMessageResponse("Unknown action");
    }
});

static InvokeResponse HandleApprovalResponse(AdaptiveCardAction action, State state)
{
    string? approvalId = TryGetString(action.Data, "approval_id");
    string? decision = TryGetString(action.Data, "decision");

    if (approvalId is not null
        && (decision == ApprovalStatus.Approved || decision == ApprovalStatus.Rejected)
        && state.Approvals.TryGetValue(approvalId, out string? currentDecision)
        && state.Approvals.TryUpdate(approvalId, decision, currentDecision))
    {
        if (state.ApprovalWaiters.TryRemove(approvalId, out TaskCompletionSource<string>? waiter))
            waiter.TrySetResult(decision);
        return AdaptiveCardResponse.CreateMessageResponse("Response recorded");
    }

    return AdaptiveCardResponse.CreateMessageResponse(
        "Unable to record response. The approval request may be invalid or expired.");
}

static InvokeResponse HandleAskReply(AdaptiveCardAction action, State state)
{
    string? requestId = TryGetString(action.Data, "request_id");
    string? reply = TryGetString(action.Data, "reply");

    if (requestId is not null
        && state.PendingAsks.TryGetValue(requestId, out PendingAsk? entry)
        && entry.Status == AskStatus.Pending)
    {
        PendingAsk answered = entry with { Status = AskStatus.Answered, Reply = reply ?? string.Empty };
        if (state.PendingAsks.TryUpdate(requestId, answered, entry))
        {
            if (state.ReplyWaiters.TryRemove(requestId, out TaskCompletionSource<PendingAsk>? waiter))
                waiter.TrySetResult(answered);
            return AdaptiveCardResponse.CreateMessageResponse("Thanks for your reply!");
        }
    }

    return AdaptiveCardResponse.CreateMessageResponse(
        "Unable to record reply. The ask may be invalid or expired.");
}

webApp.MapMcp("/mcp");
webApp.Run();

static string? TryGetString(Dictionary<string, object>? data, string key)
    => data is not null && data.TryGetValue(key, out object? value) ? value?.ToString() : null;
