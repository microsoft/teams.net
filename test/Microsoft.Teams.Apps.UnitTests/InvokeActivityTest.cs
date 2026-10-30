// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests;

public class InvokeActivityTest
{
    [Fact]
    public void DefaultCtor()
    {
        InvokeActivity ia = new();
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityTypes.Invoke, ia.Type);
        Assert.Null(ia.Name);
        Assert.Null(ia.Value);
        // Assert.Null(ia.Conversation);
    }

    [Fact]
    public void FromCoreActivityWithValue()
    {
        // Build from JSON so that conversation lands in Properties as a JsonElement
        CoreActivity coreActivity = CoreActivity.FromJsonString("""
            {
                "type": "invoke",
                "value": { "key": "value" },
                "conversation": { "id": "convId" },
                "name": "testName"
            }
            """);
        InvokeActivity ia = InvokeActivity.FromActivity(coreActivity);
        Assert.NotNull(ia);
        Assert.Equal(TeamsActivityTypes.Invoke, ia.Type);
        Assert.Equal("testName", ia.Name!.ToString());
        Assert.NotNull(ia.Value);
        Assert.Equal("convId", ia.Conversation?.Id);
        Assert.Equal("value", ia.Value?["key"]?.ToString());
    }

    [Fact]
    public void TypedWrapperOfAPromotedInvoke_KeepsTheValuesTheBaseConstructorPromoted()
    {
        InvokeActivity promoted = InvokeActivity.FromActivity(CoreActivity.FromJsonString("""
            {
                "type": "invoke",
                "name": "adaptiveCard/action",
                "id": "invoke-1",
                "replyToId": "card-1",
                "value": { "key": "value" },
                "conversation": { "id": "convId", "conversationType": "personal" },
                "channelData": { "chat": { "id": "chat-1" }, "replyChain": { "id": "100" }, "source": { "name": "message" } },
                "entities": [ { "type": "mention", "text": "<at>bot</at>" } ]
            }
            """));

        InvokeActivity<System.Text.Json.JsonElement> typed = new(promoted);

        Assert.Equal(InvokeNames.AdaptiveCardAction, typed.Name);
        Assert.Equal("card-1", typed.ReplyToId);
        Assert.Equal("100", typed.ChannelData?.ReplyChain?.Id);
        Assert.Equal("chat-1", typed.ChannelData?.Chat?.Id);
        Assert.NotNull(typed.ChannelData?.Source);
        Assert.Single(typed.Entities!);
        Assert.Equal("value", typed.Value.GetProperty("key").GetString());
        Assert.Equal(ConversationTypes.Personal, typed.Conversation?.ConversationType);
    }

    [Theory]
    [InlineData("invoke")]
    [InlineData("event")]
    [InlineData("agentLifecycle")]
    public void TypedWrapperOfAPromotedActivity_KeepsChannelDataEntitiesAndNameAndLeavesNoDuplicateKeys(string kind)
    {
        string name = kind switch { "invoke" => "adaptiveCard/action", "event" => "application/vnd.microsoft.meetingStart", _ => "agentLifecycle" };
        CoreActivity core = CoreActivity.FromJsonString($$"""
            {
                "type": "{{(kind == "invoke" ? "invoke" : "event")}}",
                "name": "{{name}}",
                "value": { "key": "value" },
                "conversation": { "id": "convId" },
                "channelData": { "chat": { "id": "chat-1" }, "replyChain": { "id": "100" }, "tenant": { "id": "tenant-1" } },
                "entities": [ { "type": "mention", "text": "<at>bot</at>" } ]
            }
            """);
        TeamsActivity promoted;
        TeamsActivity typed;
        switch (kind)
        {
            case "invoke":
                InvokeActivity invoke = InvokeActivity.FromActivity(core);
                promoted = invoke;
                typed = new InvokeActivity<System.Text.Json.JsonElement>(invoke);
                break;
            case "event":
                EventActivity evt = EventActivity.FromActivity(core);
                promoted = evt;
                typed = new EventActivity<System.Text.Json.JsonElement>(evt);
                break;
            default:
                EventActivity lifecycle = EventActivity.FromActivity(core);
                promoted = lifecycle;
                typed = new AgentLifecycleEventActivity<System.Text.Json.JsonElement>(lifecycle);
                break;
        }

        Assert.Same(promoted.ChannelData, typed.ChannelData);
        Assert.Same(promoted.Entities, typed.Entities);
        Assert.Equal("100", typed.ChannelData?.ReplyChain?.Id);
        Assert.Equal("tenant-1", typed.ChannelData?.Tenant?.Id);
        Assert.Single(typed.Entities!);
        Assert.Equal(name, typed.Type == TeamsActivityTypes.Invoke ? ((InvokeActivity)typed).Name?.ToString() : ((EventActivity)typed).Name?.ToString());
        Assert.False(typed.Properties.ContainsKey("channelData"));
        Assert.False(typed.Properties.ContainsKey("entities"));
        Assert.False(typed.Properties.ContainsKey("name"));
        Assert.False(typed.Properties.ContainsKey("value"));
    }
}
