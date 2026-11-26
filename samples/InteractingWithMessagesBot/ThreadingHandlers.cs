// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Schema;

internal static class ThreadingHandlers
{
    internal static void Register(TeamsBotApplication teamsApp)
    {
        // "thread reply" uses the app's reply mode, Combined by default: in a chat, a reply to a top-level message starts a thread under it, a reply inside a thread stays in that thread, and the inbound message is quoted.
        // "thread reply unquoted" passes ReplyMode.Thread: the same placement, without the quote when the reply lands in a thread.
        // The Quote mode is shown by the "quote reply" command.
        // In a channel every reply goes in the thread of the inbound message, so the mode only decides the quote.
        // The app-wide default is set with AddTeamsBotApplication(options => options.ReplyMode = ...).
        teamsApp.OnMessage("(?i)^thread reply$", async (context, cancellationToken) =>
        {
            await context.ReplyAsync("This reply uses the default reply mode, `Combined`. In a chat it starts a thread under your top-level message, or stays in your thread, and quotes your message.", cancellationToken);
        });

        teamsApp.OnMessage("(?i)^thread reply unquoted$", async (context, cancellationToken) =>
        {
            await context.ReplyAsync(
                "This reply uses `ReplyMode.Thread`. It is placed like `ReplyMode.Combined`, without the quote when it lands in a thread.",
                new ReplyOptions { Mode = ReplyMode.Thread },
                cancellationToken);
        });

        // In a channel, the message is sent in the same thread.
        // In a chat, it matches the level of the inbound message: in the thread when the inbound message is in one, otherwise at top level.
        // In a chat, unlike ReplyAsync, SendAsync never starts a new thread.
        teamsApp.OnMessage("(?i)^thread send$", async (context, cancellationToken) =>
        {
            await context.SendAsync("This is sent without quoting. In a channel it goes in the same thread. In a chat it matches the level of your message.", cancellationToken);
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
