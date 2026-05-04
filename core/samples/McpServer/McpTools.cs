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
public sealed class McpTools(TeamsBotApplication app, State state, IConfiguration config)
{
    [McpServerTool(Name = "notify"), Description("Send a notification to a Teams user. No response expected.")]
    public async Task<NotifyResult> Notify(
        [Description("The Teams user id (e.g. 29:...) to notify.")] string userId,
        [Description("The message text to send.")] string message,
        CancellationToken cancellationToken = default)
    {
        string conversationId = await GetOrCreateConversationAsync(userId, cancellationToken);
        await app.Send(conversationId, message, cancellationToken: cancellationToken);
        return new NotifyResult(Notified: true, UserId: userId);
    }

    [McpServerTool(Name = "ask"), Description(
        "Ask a Teams user a question. Returns a request_id — use get_reply for their response. " +
        "Only one outstanding ask per user is supported; their next message answers it.")]
    public async Task<AskResult> Ask(
        [Description("The Teams user id to ask.")] string userId,
        [Description("The question to ask.")] string question,
        CancellationToken cancellationToken = default)
    {
        string conversationId = await GetOrCreateConversationAsync(userId, cancellationToken);
        string requestId = Guid.NewGuid().ToString();
        await app.Send(conversationId, question, cancellationToken: cancellationToken);
        // The user's next message looks up these entries and flips status to 'answered'.
        state.PendingAsks[requestId] = new PendingAsk { UserId = userId };
        state.UserPendingAsk[userId] = requestId;
        return new AskResult(RequestId: requestId);
    }

    [McpServerTool(Name = "get_reply"), Description(
        "Get the reply to a question sent with ask. Returns status 'pending' until the user responds.")]
    public ReplyResult GetReply(
        [Description("The request_id returned from ask.")] string requestId)
    {
        if (!state.PendingAsks.TryGetValue(requestId, out PendingAsk? entry))
        {
            throw new InvalidOperationException($"No ask found with request_id {requestId}.");
        }
        return new ReplyResult(Status: entry.Status, Reply: entry.Reply);
    }

    [McpServerTool(Name = "request_approval"), Description(
        "Send an approval request to a Teams user. Returns an approval_id — use get_approval for the decision.")]
    public async Task<ApprovalRequestResult> RequestApproval(
        [Description("The Teams user id to ask for approval.")] string userId,
        [Description("Title of the approval request.")] string title,
        [Description("Description of what is being approved.")] string description,
        CancellationToken cancellationToken = default)
    {
        string conversationId = await GetOrCreateConversationAsync(userId, cancellationToken);
        string approvalId = Guid.NewGuid().ToString();

        Uri serviceUrl = state.LastServiceUrl
            ?? throw new InvalidOperationException(
                "No service URL cached. The bot must receive at least one activity before proactive sends work.");

        TeamsActivity activity = TeamsActivity.CreateBuilder()
            .WithType(TeamsActivityType.Message)
            .WithServiceUrl(serviceUrl)
            .WithChannelId("msteams")
            .WithConversation(new Conversation(conversationId))
            .WithAdaptiveCardAttachment(Cards.ApprovalCard(approvalId, title, description))
            .Build();

        await app.SendActivityAsync(activity, cancellationToken);
        state.Approvals[approvalId] = ApprovalStatus.Pending;
        return new ApprovalRequestResult(ApprovalId: approvalId);
    }

    [McpServerTool(Name = "get_approval"), Description(
        "Get the status of an approval request. Returns 'pending', 'approved', or 'rejected'.")]
    public ApprovalResult GetApproval(
        [Description("The approval_id returned from request_approval.")] string approvalId)
    {
        if (!state.Approvals.TryGetValue(approvalId, out string? status))
        {
            throw new InvalidOperationException($"No approval found with approval_id {approvalId}.");
        }
        return new ApprovalResult(ApprovalId: approvalId, Status: status);
    }

    // Returns the cached 1:1 conversation id for a user, or opens a new 1:1 proactively.
    private async Task<string> GetOrCreateConversationAsync(string userId, CancellationToken cancellationToken)
    {
        if (state.Conversations.TryGetValue(userId, out string? existing))
        {
            return existing;
        }

        Uri serviceUrl = state.LastServiceUrl
            ?? throw new InvalidOperationException(
                "No service URL cached. The bot must receive at least one activity before proactive sends work.");
        ConversationParameters parameters = new()
        {
            Members = [new ConversationAccount { Id = userId }],
            TenantId = config["AzureAd:TenantId"],
        };

        CreateConversationResponse resource = await app.Api
            .ForServiceUrl(serviceUrl)
            .Conversations
            .CreateAsync(parameters, cancellationToken: cancellationToken);

        string id = resource.Id
            ?? throw new InvalidOperationException("conversations.create returned no id.");
        state.Conversations[userId] = id;
        return id;
    }
}
