// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Text.Json;

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// The Teams service can send nested channel data objects as empty objects. These are inbound-only
/// models, so a missing field must not fail deserialization of the whole payload.
/// See https://github.com/microsoft/teams.py/issues/563 for the equivalent break in the Python SDK.
/// </summary>
public class TeamsChannelDataDeserializationTests
{
    [Fact]
    public void Deserialize_ChatAndReplyChainIds_ArePopulated()
    {
        const string json = """
        {
            "chat": { "id": "19:chat@unq.gbl.spaces", "encrypted": { "id": "a:encrypted-chat" } },
            "replyChain": { "id": "1772129782775" }
        }
        """;

        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
        Assert.Equal("19:chat@unq.gbl.spaces", channelData.Chat?.Id);
        Assert.Equal("a:encrypted-chat", channelData.Chat?.Encrypted?.Id);
        Assert.Equal("1772129782775", channelData.ReplyChain?.Id);
    }

    [Fact]
    public void Deserialize_MeetingChatChannelData_PopulatesMeetingId()
    {
        const string json = """
        {
            "tenant": { "id": "tenant-id" },
            "source": null,
            "meeting": { "id": "MCMxOTptZWV0aW5n==" }
        }
        """;

        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
        Assert.Equal("MCMxOTptZWV0aW5n==", channelData.Meeting?.Id);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"meeting\":null}")]
    [InlineData("{\"meeting\":{}}")]
    public void Deserialize_MeetingAbsentEmptyOrNull_DoesNotThrowAndHasNoId(string json)
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
        Assert.Null(channelData.Meeting?.Id);
    }

    [Fact]
    public void Deserialize_ChatAndReplyChainAbsent_AreNull()
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>("{\"tenant\":{\"id\":\"tenant-id\"}}");

        Assert.NotNull(channelData);
        Assert.Null(channelData.Chat);
        Assert.Null(channelData.ReplyChain);
    }

    [Theory]
    [InlineData("{\"chat\":null,\"replyChain\":null}")]
    [InlineData("{\"chat\":{},\"replyChain\":{}}")]
    [InlineData("{\"chat\":{\"encrypted\":{}}}")]
    [InlineData("{\"chat\":{\"encrypted\":null}}")]
    public void Deserialize_ChatAndReplyChainEmptyOrNull_DoesNotThrowAndHasNoIds(string json)
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
        Assert.Null(channelData.Chat?.Id);
        Assert.Null(channelData.Chat?.Encrypted?.Id);
        Assert.Null(channelData.ReplyChain?.Id);
    }

    [Fact]
    public void Deserialize_ChatAndChannelIds_AreIndependent()
    {
        const string json = """
        {
            "chat": { "id": "19:chat@thread.v2" },
            "channel": { "id": "19:channel@thread.tacv2" },
            "replyChain": { "id": "root-id" }
        }
        """;

        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.Equal("19:chat@thread.v2", channelData?.Chat?.Id);
        Assert.Equal("19:channel@thread.tacv2", channelData?.Channel?.Id);
    }

    [Fact]
    public void ChatAndReplyChainIds_AreReadOnlyAndOptional()
    {
        PropertyInfo[] properties =
        [
            typeof(TeamsChannelData).GetProperty(nameof(TeamsChannelData.Chat))!,
            typeof(TeamsChannelData).GetProperty(nameof(TeamsChannelData.ReplyChain))!,
            typeof(ChatInfo).GetProperty(nameof(ChatInfo.Id))!,
            typeof(ChatInfo).GetProperty(nameof(ChatInfo.Encrypted))!,
            typeof(EncryptedChatInfo).GetProperty(nameof(EncryptedChatInfo.Id))!,
            typeof(ReplyChainInfo).GetProperty(nameof(ReplyChainInfo.Id))!,
        ];

        foreach (PropertyInfo property in properties)
        {
            Assert.NotNull(property);
            Assert.False(property.SetMethod?.IsPublic ?? false, $"{property.DeclaringType?.Name}.{property.Name} must not have a public setter");
            Assert.Equal(NullabilityState.Nullable, new NullabilityInfoContext().Create(property).ReadState);
        }
    }

    [Fact]
    public void FromActivity_ExposesChatAndReplyChainIds()
    {
        const string json = """
        {
            "type": "message",
            "id": "1772129782999",
            "conversation": { "id": "19:chat@thread.v2" },
            "channelData": {
                "chat": { "id": "19:chat@thread.v2" },
                "replyChain": { "id": "1772129782775" }
            }
        }
        """;

        TeamsActivity activity = TeamsActivity.FromActivity(CoreActivity.FromJsonString(json));

        Assert.Equal("19:chat@thread.v2", activity.ChannelData?.Chat?.Id);
        Assert.Equal("1772129782775", activity.ChannelData?.ReplyChain?.Id);
    }

    [Theory]
    [InlineData("{\"app\":{}}")]
    [InlineData("{\"channel\":{}}")]
    [InlineData("{\"team\":{}}")]
    [InlineData("{\"tenant\":{}}")]
    [InlineData("{\"settings\":{}}")]
    public void Deserialize_EmptyNestedObject_DoesNotThrow(string json)
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
    }

    [Fact]
    public void Deserialize_EmptySettings_LeavesSelectedChannelNull()
    {
        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>("{\"settings\":{}}");

        Assert.NotNull(channelData);
        Assert.NotNull(channelData.Settings);
        Assert.Null(channelData.Settings.SelectedChannel);
    }

    [Fact]
    public void Deserialize_PopulatedChannelData_PreservesValues()
    {
        const string json = """
        {
            "app": { "id": "app-id", "version": "1.2.3" },
            "channel": { "id": "channel-id" },
            "team": { "id": "team-id" },
            "tenant": { "id": "tenant-id" },
            "settings": { "selectedChannel": { "id": "selected-channel-id" } }
        }
        """;

        TeamsChannelData? channelData = JsonSerializer.Deserialize<TeamsChannelData>(json);

        Assert.NotNull(channelData);
        Assert.Equal("app-id", channelData.App?.Id);
        Assert.Equal("1.2.3", channelData.App?.Version);
        Assert.Equal("channel-id", channelData.Channel?.Id);
        Assert.Equal("team-id", channelData.Team?.Id);
        Assert.Equal("tenant-id", channelData.Tenant?.Id);
        Assert.Equal("selected-channel-id", channelData.Settings?.SelectedChannel?.Id);
    }
}
