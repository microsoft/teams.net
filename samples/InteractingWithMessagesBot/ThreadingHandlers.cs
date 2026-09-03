// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

internal static class ThreadingHandlers
{
    internal static void Register(TeamsBotApplication teamsApp)
    {
        teamsApp.OnMessage("(?i)^thread send$", async (context, cancellationToken) =>
        {
            await context.SendAsync("This is sent to the same thread, without quoting.", cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread reply$", async (context, cancellationToken) =>
        {
            (string conversationId, string threadRootId) = GetThreadReference(context.Activity);
            await teamsApp.ReplyAsync(
                conversationId,
                threadRootId,
                "This is a threaded reply to your message.",
                cancellationToken: cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread proactive$", async (context, cancellationToken) =>
        {
            (string conversationId, string threadRootId) = GetThreadReference(context.Activity);
            await teamsApp.ReplyAsync(
                conversationId,
                threadRootId,
                "This is a proactive threaded reply using teamsApp.ReplyAsync().",
                cancellationToken: cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread manual$", async (context, cancellationToken) =>
        {
            (string conversationId, string threadRootId) = GetThreadReference(context.Activity);
            await teamsApp.ReplyAsync(
                conversationId,
                threadRootId,
                new MessageActivityInput().WithText(
                    "This was sent using teamsApp.ReplyAsync() for explicit thread placement."),
                cancellationToken: cancellationToken);
        });
    }

    private static (string ConversationId, string ThreadRootId) GetThreadReference(TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity.Conversation);
        ArgumentException.ThrowIfNullOrEmpty(activity.Id);

        string inboundConversationId = activity.Conversation.Id;
        string conversationId = activity.Conversation.ThreadId();
        string[] threadParts = inboundConversationId.Split(";messageid=");
        string threadRootId = activity.ChannelData?.Thread?.Id
            ?? (threadParts.Length > 1 ? threadParts[1] : activity.Id);
        return (conversationId, threadRootId);
    }
}
