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
            await context.ReplyAsync("This is a quoted reply, matching the threading level of your message.", cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread send$", async (context, cancellationToken) =>
        {
            await context.SendAsync("This is sent without quoting, matching the threading level of your message.", cancellationToken);
        });

        // The reply chain is taken from ChannelData.ReplyChain.Id.
        // A proactive send runs without the inbound activity, so an agentic user must be named explicitly.
        // The identity is null when the inbound recipient is not an agentic user.
        teamsApp.OnMessage("(?i)^thread proactive$", async (context, cancellationToken) =>
        {
            (string? conversationId, string? replyChainId) = GetThreadReference(context.Activity);
            if (conversationId is null || replyChainId is null)
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
            if (conversationId is null)
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
        "Not enough thread metadata on this message for the sample to address a thread: it reads `ChannelData.Chat.Id` or `ChannelData.Channel.Id` for the conversation id and, in a chat, `ChannelData.ReplyChain.Id` for the root.";

    // The ids come from the payload.
    // Chat.Id identifies the chat and Channel.Id the channel.
    // ReplyChain.Id is the root of the reply chain.
    // An id the payload does not carry is null.
    private static (string? ConversationId, string? ReplyChainId) GetThreadReference(TeamsActivity activity)
    {
        string? conversationId = activity.ChannelData?.Chat?.Id ?? activity.ChannelData?.Channel?.Id;
        return (conversationId, activity.ChannelData?.ReplyChain?.Id);
    }
}
