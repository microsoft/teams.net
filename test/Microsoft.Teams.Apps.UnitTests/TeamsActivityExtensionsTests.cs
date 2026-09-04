// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class TeamsActivityExtensionsTests
{
    [Fact]
    public void GetProactiveThreadReference_PrefersTypedThreadId()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation;messageid=legacy-root",
            "inbound-id",
            ConversationTypes.GroupChat,
            "typed-root");

        (string conversationId, string threadRootId) = activity.GetProactiveThreadReference();

        Assert.Equal("base-conversation", conversationId);
        Assert.Equal("typed-root", threadRootId);
    }

    [Fact]
    public void GetProactiveThreadReference_UsesLegacyThreadId()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation;messageid=legacy-root",
            "inbound-id",
            ConversationTypes.GroupChat);

        (string conversationId, string threadRootId) = activity.GetProactiveThreadReference();

        Assert.Equal("base-conversation", conversationId);
        Assert.Equal("legacy-root", threadRootId);
    }

    [Fact]
    public void GetProactiveThreadReference_FallsBackToInboundActivityId()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation",
            "inbound-id",
            ConversationTypes.GroupChat);

        (string conversationId, string threadRootId) = activity.GetProactiveThreadReference();

        Assert.Equal("base-conversation", conversationId);
        Assert.Equal("inbound-id", threadRootId);
    }

    [Fact]
    public void GetProactiveThreadReference_ThrowsWhenFallbackActivityIdIsMissing()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation",
            null,
            ConversationTypes.GroupChat);

        Assert.Throws<ArgumentNullException>(() => activity.GetProactiveThreadReference());
    }

    [Fact]
    public void GetDefaultThreadId_PrefersTypedThreadId()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation;messageid=legacy-root",
            "inbound-id",
            ConversationTypes.Channel,
            "typed-root");

        Assert.Equal("typed-root", activity.GetDefaultThreadId());
    }

    [Fact]
    public void GetDefaultThreadId_UsesLegacyThreadId()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation;messageid=legacy-root",
            "inbound-id",
            ConversationTypes.GroupChat);

        Assert.Equal("legacy-root", activity.GetDefaultThreadId());
    }

    [Fact]
    public void GetDefaultThreadId_UsesInboundActivityIdForChannelRoot()
    {
        MessageActivity activity = BuildActivity(
            "base-conversation",
            "inbound-id",
            ConversationTypes.Channel);

        Assert.Equal("inbound-id", activity.GetDefaultThreadId());
    }

    [Theory]
    [InlineData("groupChat")]
    [InlineData("personal")]
    public void GetDefaultThreadId_ReturnsNullForUnthreadedChatRoot(string conversationType)
    {
        MessageActivity activity = BuildActivity(
            "base-conversation",
            "inbound-id",
            new ConversationType(conversationType));

        Assert.Null(activity.GetDefaultThreadId());
    }

    [Obsolete]
    private static MessageActivity BuildActivity(
        string conversationId,
        string? activityId,
        ConversationType conversationType,
        string? threadId = null)
    {
        MessageActivity activity = new("test")
        {
            Id = activityId,
            Conversation = new TeamsConversation
            {
                Id = conversationId,
                ConversationType = conversationType,
            },
        };

        if (threadId is not null)
        {
            activity.ChannelData = JsonSerializer.Deserialize<TeamsChannelData>(
                $"{{\"thread\":{{\"id\":\"{threadId}\"}}}}");
        }

        return activity;
    }
}
