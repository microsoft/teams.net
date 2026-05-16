// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Core.Diagnostics;
using Microsoft.Teams.Core.Schema;
using OpenTelemetry;

namespace Microsoft.Teams.Core.UnitTests.Diagnostics;

public class CoreBaggageBuilderTests
{
    [Fact]
    public void FromCoreActivity_PopulatesExpectedKeysFromTypedFields()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            Id = "act-1",
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://smba.example/"),
            Conversation = new("conv-1"),
            From = new() { Id = "from-1", Name = "User One" },
            Recipient = new()
            {
                Id = "agent-id",
                Name = "Agent",
                AgenticAppId = "agentic-app-1",
                AgenticUserId = "auid-1",
                AgenticAppBlueprintId = "blueprint-1",
                TenantId = "tenant-1",
            },
        };

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromCoreActivity(activity));

        Assert.Equal("tenant-1", baggage["microsoft.tenant.id"]);
        Assert.Equal("conv-1", baggage["gen_ai.conversation.id"]);
        Assert.Equal("https://smba.example/", baggage["microsoft.conversation.item.link"]);
        Assert.Equal("msteams", baggage["microsoft.channel.name"]);
        Assert.Equal("agentic-app-1", baggage["gen_ai.agent.id"]); // AgenticAppId wins over Id
        Assert.Equal("Agent", baggage["gen_ai.agent.name"]);
        Assert.Equal("auid-1", baggage["microsoft.agent.user.id"]);
        Assert.Equal("blueprint-1", baggage["microsoft.a365.agent.blueprint.id"]);
        Assert.Equal("User One", baggage["user.name"]);
    }

    [Fact]
    public void FromCoreActivity_AgentIdFallsBackToRecipientIdWhenNoAgenticAppId()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            ChannelId = "msteams",
            Conversation = new("conv-x"),
            Recipient = new() { Id = "plain-recipient-id", Name = "Bot" },
        };

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromCoreActivity(activity));

        Assert.Equal("plain-recipient-id", baggage["gen_ai.agent.id"]);
    }

    [Fact]
    public void FromCoreActivity_FallsBackToChannelDataTenantIdWhenRecipientTenantIdIsNull()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            ChannelId = "msteams",
            Conversation = new("conv-1"),
            Recipient = new() { Id = "r", Name = "R" /* no TenantId */ },
        };
        // Plant a channelData object with tenant.id, simulating classic Teams Bot Framework JSON.
        JsonElement channelData = JsonSerializer.SerializeToElement(new
        {
            tenant = new { id = "tenant-from-channeldata" },
        });
        activity.Properties["channelData"] = channelData;

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromCoreActivity(activity));

        Assert.Equal("tenant-from-channeldata", baggage["microsoft.tenant.id"]);
    }

    [Fact]
    public void FromCoreActivity_DoesNotEmitChannelLink()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            ChannelId = "msteams",
            Conversation = new("conv-1"),
            Recipient = new() { Id = "r" },
        };

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromCoreActivity(activity));

        Assert.False(baggage.ContainsKey("microsoft.channel.link"));
    }

    [Fact]
    public void Build_NullAndWhitespaceValuesAreSkipped()
    {
        Dictionary<string, string?> baggage = ApplyAndCapture(b => b
            .ConversationId(null)
            .ConversationId("   ")
            .ConversationId("conv-keep")
            .TenantId(""));

        Assert.Equal("conv-keep", baggage["gen_ai.conversation.id"]);
        Assert.False(baggage.ContainsKey("microsoft.tenant.id"));
    }

    [Fact]
    public void Build_DisposeRestoresPreviousBaggage()
    {
        Baggage initial = Baggage.Current.SetBaggage("preexisting", "yes");
        Baggage.Current = initial;
        try
        {
            using (new CoreBaggageBuilder().TenantId("tenant-x").Build())
            {
                Assert.Equal("tenant-x", Baggage.GetBaggage("microsoft.tenant.id"));
                Assert.Equal("yes", Baggage.GetBaggage("preexisting"));
            }

            Assert.Null(Baggage.GetBaggage("microsoft.tenant.id"));
            Assert.Equal("yes", Baggage.GetBaggage("preexisting"));
        }
        finally
        {
            Baggage.Current = default;
        }
    }

    [Fact]
    public void OperationSource_SetsServiceName()
    {
        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.OperationSource("teams-bot"));
        Assert.Equal("teams-bot", baggage["service.name"]);
    }

    [Fact]
    public void InvokeAgentServer_OmitsPortWhen443()
    {
        Dictionary<string, string?> baggage443 = ApplyAndCapture(b => b.InvokeAgentServer("api.example.com", 443));
        Assert.Equal("api.example.com", baggage443["server.address"]);
        Assert.False(baggage443.ContainsKey("server.port"));

        Dictionary<string, string?> baggage8080 = ApplyAndCapture(b => b.InvokeAgentServer("api.example.com", 8080));
        Assert.Equal("8080", baggage8080["server.port"]);
    }

    [Fact]
    public void Set_EscapeHatchAcceptsAnyKey()
    {
        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.Set("user.id", "aad-123"));
        Assert.Equal("aad-123", baggage["user.id"]);
    }

    private static Dictionary<string, string?> ApplyAndCapture(Action<CoreBaggageBuilder> configure)
    {
        Baggage previous = Baggage.Current;
        Baggage.Current = default;
        try
        {
            CoreBaggageBuilder builder = new();
            configure(builder);
            using (builder.Build())
            {
                Dictionary<string, string?> snapshot = new(StringComparer.Ordinal);
                foreach (KeyValuePair<string, string> kvp in Baggage.Current.GetBaggage())
                {
                    snapshot[kvp.Key] = kvp.Value;
                }
                return snapshot;
            }
        }
        finally
        {
            Baggage.Current = previous;
        }
    }
}
