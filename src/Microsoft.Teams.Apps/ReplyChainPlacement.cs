// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps;

/// <summary>
/// The chat or channel and reply chain (thread) a response to an inbound message is posted to.
/// </summary>
/// <param name="ChatOrChannelId">The chat or channel id, <c>channelData.chat.id</c> or <c>channelData.channel.id</c>, taken from the inbound payload.</param>
/// <param name="ReplyChainId">The id of the message at the root of the reply chain, <c>channelData.replyChain.id</c>.</param>
/// <param name="InChannel">Whether <paramref name="ChatOrChannelId"/> is a channel.</param>
internal readonly record struct ReplyChainPlacement(string ChatOrChannelId, string ReplyChainId, bool InChannel = false);
