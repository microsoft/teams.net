// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

internal static class ThreadingHandlers
{
    internal static void Register(TeamsBotApplication teamsApp)
    {
        teamsApp.OnMessage("(?i)^thread reply$", async (context, cancellationToken) =>
        {
            await context.ReplyAsync("This is a quoted reply. In a channel it goes in the same thread. In a 1:1 chat it matches the level of your message.", cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread send$", async (context, cancellationToken) =>
        {
            await context.SendAsync("This is sent without quoting. In a channel it goes in the same thread. In a 1:1 chat it matches the level of your message.", cancellationToken);
        });

        // The reply chain is taken from ChannelData.ReplyChain.Id.
        // A proactive send runs without the inbound activity, so an agentic user must be named explicitly.
        // The identity is null when the inbound recipient is not an agentic user.
        teamsApp.OnMessage("(?i)^thread proactive$", async (context, cancellationToken) =>
        {
            (string? conversationId, string? replyChainId) = GetThreadReference(context.Activity);
            if (string.IsNullOrWhiteSpace(conversationId) || string.IsNullOrWhiteSpace(replyChainId))
            {
                await context.SendAsync(
                    new MessageActivityInput().WithText(InsufficientMetadata, TextFormats.Markdown),
                    cancellationToken);
                return;
            }

            await teamsApp.ReplyAsync(
                conversationId,
                replyChainId,
                new MessageActivityInput().WithText(
                    "This is a proactive reply in a thread, using `teamsApp.ReplyAsync()`.",
                    TextFormats.Markdown),
                agenticIdentity: context.Activity.Recipient?.GetAgenticIdentity(),
                cancellationToken: cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread top-level$", async (context, cancellationToken) =>
        {
            (string? conversationId, _) = GetThreadReference(context.Activity);
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                await context.SendAsync(
                    new MessageActivityInput().WithText(InsufficientMetadata, TextFormats.Markdown),
                    cancellationToken);
                return;
            }

            await teamsApp.SendAsync(
                conversationId,
                new MessageActivityInput().WithText(
                    "This is a new top-level message, sent with `teamsApp.SendAsync()` to the id of the chat or channel.",
                    TextFormats.Markdown),
                agenticIdentity: context.Activity.Recipient?.GetAgenticIdentity(),
                cancellationToken: cancellationToken);
        });
    }

    private const string InsufficientMetadata =
        "Not enough thread metadata on this message for the sample to address a thread: it reads `ChannelData.Chat.Id` or `ChannelData.Channel.Id` for the conversation id and `ChannelData.ReplyChain.Id` for the root.";

    // The ids come from the payload.
    // Chat.Id identifies the chat and Channel.Id the channel.
    // ReplyChain.Id is the root of the reply chain.
    // A post that starts a thread in a channel carries a ReplyChain.Id equal to its own id.
    // A payload without ReplyChain.Id does not establish a root, so the sample does not infer one from the channel or the activity id.
    // An id the payload does not carry is null.
    private static (string? ConversationId, string? ReplyChainId) GetThreadReference(TeamsActivity activity)
    {
        string? conversationId = activity.ChannelData?.Chat?.Id ?? activity.ChannelData?.Channel?.Id;
        string? replyChainId = activity.ChannelData?.ReplyChain?.Id;
        return (conversationId, replyChainId);
    }
}
