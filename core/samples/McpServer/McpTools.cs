// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core;
using Microsoft.Teams.Core.Schema;
using ModelContextProtocol.Server;

namespace McpServer;

// [McpServerToolType] marks the class for tool discovery; each
// [McpServerTool] method below becomes one tool surfaced to MCP clients.
// The class is registered via WithTools<McpTools>() in Program.cs.
[McpServerToolType]
public sealed class McpTools(TeamsBotApplication app, State state, IConfiguration config, GraphClient graph)
{
    [McpServerTool(Name = "notify"), Description("Send a notification to a Teams user. No response expected.")]
    public async Task<NotifyResult> Notify(
        [Description("The AAD object id of the Teams user to notify.")] string userId,
        [Description("The message text to send.")] string message,
        CancellationToken cancellationToken = default)
    {
        string conversationId = await GetOrCreateConversationAsync(userId, cancellationToken);
        TeamsActivity notifyActivity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithServiceUrl(state.ServiceUrl)
            .WithConversation(new Conversation(conversationId))
            .WithText(message)
            .Build();
        await app.SendActivityAsync(notifyActivity, cancellationToken);
        return new NotifyResult(Notified: true, UserId: userId);
    }

    [McpServerTool(Name = "ask"), Description(
        "Ask a Teams user a question. Returns a request_id — call wait_for_reply with it to get the answer.")]
    public async Task<AskResult> Ask(
        [Description("The AAD object id of the Teams user to ask.")] string userId,
        [Description("The question to ask.")] string question,
        CancellationToken cancellationToken = default)
    {
        string conversationId = await GetOrCreateConversationAsync(userId, cancellationToken);
        string requestId = Guid.NewGuid().ToString();

        // Record the pending ask before sending, so a fast reply is never lost.
        state.PendingAsks[requestId] = new PendingAsk(userId);
        TeamsActivity askActivity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithServiceUrl(state.ServiceUrl)
            .WithConversation(new Conversation(conversationId))
            .WithAdaptiveCardAttachment(Cards.AskCard(requestId, question))
            .Build();
        try
        {
            await app.SendActivityAsync(askActivity, cancellationToken);
        }
        catch
        {
            state.PendingAsks.TryRemove(requestId, out _);
            throw;
        }
        return new AskResult(RequestId: requestId);
    }

    [McpServerTool(Name = "get_reply"), Description(
        "Snapshot the current reply state for an ask. this exists for manual polling. " +
        "Returns status 'pending' until the user responds.")]
    public ReplyResult GetReply(
        [Description("The request_id returned from ask.")] string requestId)
    {
        if (!state.PendingAsks.TryGetValue(requestId, out PendingAsk? entry))
        {
            throw new InvalidOperationException($"No ask found with request_id {requestId}.");
        }
        return new ReplyResult(Status: entry.Status, Reply: entry.Reply);
    }

    [McpServerTool(Name = "wait_for_reply"), Description(
        "Wait for the user's reply to an earlier ask. Blocks up to timeout_seconds (default 30). " +
        "Returns the reply when it arrives, or status='pending' if the timeout fires")]
    public async Task<ReplyResult> WaitForReply(
        [Description("The request_id returned from ask.")] string requestId,
        [Description("Max seconds to wait before returning (default 30).")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        if (!state.PendingAsks.TryGetValue(requestId, out PendingAsk? entry))
        {
            throw new InvalidOperationException($"No ask found with request_id {requestId}.");
        }
        if (entry.Status != AskStatus.Pending)
        {
            return new ReplyResult(entry.Status, entry.Reply);
        }

        TaskCompletionSource<PendingAsk> waiter = state.ReplyWaiters.GetOrAdd(
            requestId,
            _ => new TaskCompletionSource<PendingAsk>(TaskCreationOptions.RunContinuationsAsynchronously));

        // Re-check after registering the waiter so we don't miss a signal that
        // fired between the initial read and GetOrAdd.
        if (state.PendingAsks.TryGetValue(requestId, out PendingAsk? latest)
            && latest.Status != AskStatus.Pending)
        {
            return new ReplyResult(latest.Status, latest.Reply);
        }

        try
        {
            PendingAsk result = await waiter.Task.WaitAsync(
                TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);
            return new ReplyResult(result.Status, result.Reply);
        }
        catch (TimeoutException)
        {
            state.PendingAsks.TryGetValue(requestId, out PendingAsk? current);
            return new ReplyResult(current?.Status ?? AskStatus.Pending, current?.Reply);
        }
    }

    [McpServerTool(Name = "request_approval"), Description(
        "Send an approval request to a Teams user. Returns an approval_id — call wait_for_approval with it to get the decision.")]
    public async Task<ApprovalRequestResult> RequestApproval(
        [Description("The AAD object id of the Teams user to ask for approval.")] string userId,
        [Description("Title of the approval request.")] string title,
        [Description("Description of what is being approved.")] string description,
        CancellationToken cancellationToken = default)
    {
        string conversationId = await GetOrCreateConversationAsync(userId, cancellationToken);
        string approvalId = Guid.NewGuid().ToString();

        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithServiceUrl(state.ServiceUrl)
            .WithConversation(new Conversation(conversationId))
            .WithAdaptiveCardAttachment(Cards.ApprovalCard(approvalId, title, description))
            .Build();

        state.Approvals[approvalId] = ApprovalStatus.Pending;
        try
        {
            await app.SendActivityAsync(activity, cancellationToken);
        }
        catch
        {
            state.Approvals.TryRemove(approvalId, out _);
            throw;
        }

        return new ApprovalRequestResult(ApprovalId: approvalId);
    }

    [McpServerTool(Name = "get_approval"), Description(
        "Snapshot the current status of an approval request. this exists for manual polling." +
        "Returns 'pending', 'approved', or 'rejected'.")]
    public ApprovalResult GetApproval(
        [Description("The approval_id returned from request_approval.")] string approvalId)
    {
        if (!state.Approvals.TryGetValue(approvalId, out string? status))
        {
            throw new InvalidOperationException($"No approval found with approval_id {approvalId}.");
        }
        return new ApprovalResult(ApprovalId: approvalId, Status: status);
    }

    [McpServerTool(Name = "wait_for_approval"), Description(
        "Wait for an approval decision. Blocks up to timeout_seconds (default 30). " +
        "Returns 'approved' or 'rejected' when the user clicks, or 'pending' if the timeout fires.")]
    public async Task<ApprovalResult> WaitForApproval(
        [Description("The approval_id returned from request_approval.")] string approvalId,
        [Description("Max seconds to wait before returning (default 30).")] int timeoutSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        if (!state.Approvals.TryGetValue(approvalId, out string? status))
        {
            throw new InvalidOperationException($"No approval found with approval_id {approvalId}.");
        }
        if (status != ApprovalStatus.Pending)
        {
            return new ApprovalResult(approvalId, status);
        }

        TaskCompletionSource<string> waiter = state.ApprovalWaiters.GetOrAdd(
            approvalId,
            _ => new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously));

        if (state.Approvals.TryGetValue(approvalId, out string? latest) && latest != ApprovalStatus.Pending)
        {
            return new ApprovalResult(approvalId, latest);
        }

        try
        {
            string result = await waiter.Task.WaitAsync(
                TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);
            return new ApprovalResult(approvalId, result);
        }
        catch (TimeoutException)
        {
            state.Approvals.TryGetValue(approvalId, out string? current);
            return new ApprovalResult(approvalId, current ?? ApprovalStatus.Pending);
        }
    }

    [McpServerTool(Name = "find_user"), Description(
        "Find users in this tenant by partial name, email, or UPN. " +
        "Returns up to 5 matches with their AAD object ids — pass an id to " +
        "notify, ask, or request_approval.")]
    public async Task<FindUserResult> FindUser(
        [Description("Name, email, or UPN fragment to search for.")] string query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserMatch> matches = await graph.SearchUsersAsync(query, top: 5, cancellationToken);
        return new FindUserResult(matches);
    }

    // Returns the cached 1:1 conversation id for a user, or opens a new 1:1 proactively.
    private async Task<string> GetOrCreateConversationAsync(string userId, CancellationToken cancellationToken)
    {
        if (state.Conversations.TryGetValue(userId, out string? existing))
        {
            return existing;
        }

        ConversationParameters parameters = new()
        {
            Members = [new ConversationAccount { Id = userId }],
            TenantId = config["AzureAd:TenantId"],
        };

        CreateConversationResponse resource = await app.Api
            .ForServiceUrl(state.ServiceUrl)
            .Conversations
            .CreateAsync(parameters, cancellationToken: cancellationToken);

        string id = resource.Id
            ?? throw new InvalidOperationException("conversations.create returned no id.");
        state.Conversations[userId] = id;
        return id;
    }
}
