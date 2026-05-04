// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Diagnostics;
using Microsoft.Teams.Apps.Schema;
using Microsoft.Teams.Core.Schema;
using OpenTelemetry;

namespace Microsoft.Teams.Apps.UnitTests.Diagnostics;

public class BaggageBuilderTests
{
    [Fact]
    public void FromTeamsContext_PopulatesAppsOnlyKeysFromTeamsConversationAccount()
    {
        TeamsConversationAccount from = new() { Id = "from-id", Name = "User One" };
        from.AadObjectId = "aad-from";
        from.Email = "user@contoso.com";

        TeamsConversationAccount recipient = new()
        {
            Id = "agent-id",
            Name = "Agent",
            AgenticAppId = "agentic-app-1",
            AgenticUserId = "auid-1",
            AgenticAppBlueprintId = "blueprint-1",
            TenantId = "tenant-1",
        };
        recipient.UserRole = "agent";
        recipient.Email = "agent@contoso.com";

        MessageActivity activity = new()
        {
            Id = "act-1",
            ChannelId = "msteams",
            ServiceUrl = new Uri("https://smba.example/"),
            Conversation = new TeamsConversation { Id = "conv-1" },
            From = from,
            Recipient = recipient,
        };

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromTeamsContext(BuildCtx(activity)));

        // Apps-only keys
        Assert.Equal("aad-from", baggage["user.id"]);
        Assert.Equal("user@contoso.com", baggage["user.email"]);
        Assert.Equal("agent@contoso.com", baggage["microsoft.agent.user.email"]);
        Assert.Equal("agent", baggage["gen_ai.agent.description"]);

        // Inherited from CoreActivity-shaped fields
        Assert.Equal("tenant-1", baggage["microsoft.tenant.id"]);
        Assert.Equal("conv-1", baggage["gen_ai.conversation.id"]);
        Assert.Equal("https://smba.example/", baggage["microsoft.conversation.item.link"]);
        Assert.Equal("msteams", baggage["microsoft.channel.name"]);
        Assert.Equal("agentic-app-1", baggage["gen_ai.agent.id"]);
        Assert.Equal("Agent", baggage["gen_ai.agent.name"]);
        Assert.Equal("auid-1", baggage["microsoft.agent.user.id"]);
        Assert.Equal("blueprint-1", baggage["microsoft.a365.agent.blueprint.id"]);
        Assert.Equal("User One", baggage["user.name"]);
    }

    [Fact]
    public void FromTeamsContext_FallsBackToTypedChannelDataTenantId()
    {
        MessageActivity activity = new()
        {
            Id = "act-1",
            ChannelId = "msteams",
            Conversation = new TeamsConversation { Id = "conv-1" },
            Recipient = new TeamsConversationAccount { Id = "agent" /* no TenantId */ },
            ChannelData = new TeamsChannelData
            {
                Tenant = new TeamsChannelDataTenant { Id = "tenant-from-channeldata" },
            },
        };

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromTeamsContext(BuildCtx(activity)));

        Assert.Equal("tenant-from-channeldata", baggage["microsoft.tenant.id"]);
    }

    [Fact]
    public void FromTeamsContext_DoesNotEmitChannelLink()
    {
        MessageActivity activity = new()
        {
            Id = "act-1",
            ChannelId = "msteams",
            Conversation = new TeamsConversation { Id = "conv-1" },
            Recipient = new TeamsConversationAccount { Id = "agent", TenantId = "t" },
        };

        Dictionary<string, string?> baggage = ApplyAndCapture(b => b.FromTeamsContext(BuildCtx(activity)));

        Assert.False(baggage.ContainsKey("microsoft.channel.link"));
    }

    [Fact]
    public void Build_DisposeRestoresPreviousBaggage()
    {
        Baggage previous = Baggage.Current;
        Baggage.Current = default;
        try
        {
            using (new BaggageBuilder().UserId("u").UserEmail("u@example.com").Build())
            {
                Assert.Equal("u", Baggage.GetBaggage("user.id"));
                Assert.Equal("u@example.com", Baggage.GetBaggage("user.email"));
            }

            Assert.Null(Baggage.GetBaggage("user.id"));
            Assert.Null(Baggage.GetBaggage("user.email"));
        }
        finally
        {
            Baggage.Current = previous;
        }
    }

    [Fact]
    public void AppsOnlySetters_SetExpectedKeys()
    {
        Dictionary<string, string?> baggage = ApplyAndCapture(b => b
            .UserId("u-id")
            .UserEmail("u@x.com")
            .AgentDescription("agent")
            .AgenticUserEmail("a@x.com"));

        Assert.Equal("u-id", baggage["user.id"]);
        Assert.Equal("u@x.com", baggage["user.email"]);
        Assert.Equal("agent", baggage["gen_ai.agent.description"]);
        Assert.Equal("a@x.com", baggage["microsoft.agent.user.email"]);
    }

    private static Context<TeamsActivity> BuildCtx(TeamsActivity activity) => new(null!, activity);

    private static Dictionary<string, string?> ApplyAndCapture(Action<BaggageBuilder> configure)
    {
        Baggage previous = Baggage.Current;
        Baggage.Current = default;
        try
        {
            BaggageBuilder builder = new();
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
