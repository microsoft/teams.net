// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps;

/// <summary>
/// How the context's <see cref="Context{TActivity}.ReplyAsync(MessageActivityInput, ReplyOptions?, CancellationToken)"/> places and quotes its response in a chat or channel.
/// </summary>
/// <remarks>
/// In a channel every reply already goes into the thread of the inbound message, so the mode only decides the quote: <see cref="Thread"/> drops it, <see cref="Combined"/> and <see cref="Quote"/> keep it.
/// The mode applies to message activities only.
/// It does not affect <see cref="TeamsBotApplication.ReplyAsync(string, string, TeamsActivityInput, Uri?, AgenticIdentity?, CancellationToken)"/>, <see cref="Context{TActivity}.SendAsync(MessageActivityInput, CancellationToken)"/>, <see cref="Context{TActivity}.QuoteAsync(string, MessageActivityInput, CancellationToken)"/>, <see cref="Context{TActivity}.TypingAsync(CancellationToken)"/>, streams or plans.
/// When the inbound activity carries no <c>channelData.chat.id</c> and <c>channelData.replyChain.id</c> (e.g. in a meeting), a reply is never threaded and keeps the quote in every mode.
/// The reply quotes the inbound activity only when that activity can be quoted, so a reply to a card action or another invoke is sent without a quote.
/// This API is in preview and may change in the future. Diagnostic: ExperimentalTeamsThreading
/// </remarks>
public enum ReplyMode
{
    /// <summary>
    /// In a chat, a reply to a top-level message goes into a thread under that message, and a reply inside a thread stays in that thread.
    /// The inbound message is quoted.
    /// </summary>
    Combined,

    /// <summary>
    /// The reply stays at the level of the inbound message (top level or the same thread) and quotes it.
    /// </summary>
    Quote,

    /// <summary>
    /// Like <see cref="Combined"/>, without the quote.
    /// </summary>
    Thread,
}
