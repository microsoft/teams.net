// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Http;

public class BotRequestPropertiesTests
{
    private static AgenticIdentity Agentic(string appId = "agentic-app", string userId = "agentic-user")
        => new() { AgenticAppId = appId, AgenticUserId = userId };

    // ---- ForBotAppId -------------------------------------------------------

    [Fact]
    public void ForBotAppId_WithValue_UsesValueAsIs()
    {
        IReadOnlyDictionary<string, object?>? bag = BotRequestProperties.ForBotAppId("28:abc");

        Assert.NotNull(bag);
        // ForBotAppId does NOT strip the channel prefix; the caller passes the id directly.
        Assert.Equal("28:abc", bag!.GetBotAppId());
        Assert.Null(bag.GetAgenticIdentity());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ForBotAppId_WithNullOrEmpty_ReturnsNull(string? botAppId)
    {
        Assert.Null(BotRequestProperties.ForBotAppId(botAppId));
    }

    // ---- ForAgenticIdentity ------------------------------------------------

    [Fact]
    public void ForAgenticIdentity_WithValue_CarriesOnlyAgenticIdentity()
    {
        AgenticIdentity identity = Agentic();

        IReadOnlyDictionary<string, object?>? bag = BotRequestProperties.ForAgenticIdentity(identity);

        Assert.NotNull(bag);
        Assert.Same(identity, bag!.GetAgenticIdentity());
        Assert.Null(bag.GetBotAppId());
    }

    [Fact]
    public void ForAgenticIdentity_WithNull_ReturnsNull()
    {
        Assert.Null(BotRequestProperties.ForAgenticIdentity(null));
    }

    // ---- FromActivity (outbound: derive from From) -------------------------

    [Fact]
    public void FromActivity_StripsChannelPrefixFromBotId_AndDerivesAgenticFromSender()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount
            {
                Id = "28:bot-app-id",
                AgenticAppId = "agentic-app",
                AgenticUserId = "agentic-user",
            },
        };

        IReadOnlyDictionary<string, object?>? bag = BotRequestProperties.FromActivity(activity);

        Assert.NotNull(bag);
        Assert.Equal("bot-app-id", bag!.GetBotAppId());
        Assert.Equal("agentic-app", bag.GetAgenticIdentity()?.AgenticAppId);
    }

    [Fact]
    public void FromActivity_WithoutChannelPrefix_KeepsIdAsIs()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "plain-bot-id" },
        };

        IReadOnlyDictionary<string, object?>? bag = BotRequestProperties.FromActivity(activity);

        Assert.NotNull(bag);
        Assert.Equal("plain-bot-id", bag!.GetBotAppId());
        // No agentic fields on the sender -> no agentic identity.
        Assert.Null(bag.GetAgenticIdentity());
    }

    [Fact]
    public void FromActivity_WithNullActivity_ReturnsNull()
    {
        Assert.Null(BotRequestProperties.FromActivity(null));
    }

    [Fact]
    public void FromActivity_WithNothingDerivable_ReturnsNull()
    {
        CoreActivity activity = new() { Type = ActivityType.Message, From = new ChannelAccount { Id = "" } };

        Assert.Null(BotRequestProperties.FromActivity(activity));
    }

    // ---- FromInboundActivity (inbound: bot app id from Recipient) ----------

    [Fact]
    public void FromInboundActivity_TakesBotAppIdFromRecipient_AndAgenticFromSender()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id", AgenticUserId = "agentic-user" },
            Recipient = new ChannelAccount { Id = "28:recipient-bot-id" },
        };

        IReadOnlyDictionary<string, object?>? bag = BotRequestProperties.FromInboundActivity(activity);

        Assert.NotNull(bag);
        Assert.Equal("recipient-bot-id", bag!.GetBotAppId());
        Assert.Equal("agentic-user", bag.GetAgenticIdentity()?.AgenticUserId);
    }

    // ---- Merge -------------------------------------------------------------

    [Fact]
    public void Merge_OverridesWinOnConflictingKeys()
    {
        IReadOnlyDictionary<string, object?>? baseBag = BotRequestProperties.ForBotAppId("base-bot");
        IReadOnlyDictionary<string, object?>? overrides = BotRequestProperties.ForBotAppId("override-bot");

        IReadOnlyDictionary<string, object?>? merged = BotRequestProperties.Merge(baseBag, overrides);

        Assert.NotNull(merged);
        Assert.Equal("override-bot", merged!.GetBotAppId());
    }

    [Fact]
    public void Merge_UnionsDistinctKeys()
    {
        AgenticIdentity identity = Agentic();
        IReadOnlyDictionary<string, object?>? baseBag = BotRequestProperties.ForBotAppId("bot-1");
        IReadOnlyDictionary<string, object?>? overrides = BotRequestProperties.ForAgenticIdentity(identity);

        IReadOnlyDictionary<string, object?>? merged = BotRequestProperties.Merge(baseBag, overrides);

        Assert.NotNull(merged);
        Assert.Equal("bot-1", merged!.GetBotAppId());
        Assert.Same(identity, merged.GetAgenticIdentity());
    }

    [Fact]
    public void Merge_WithNullBase_ReturnsOverrides()
    {
        IReadOnlyDictionary<string, object?>? overrides = BotRequestProperties.ForBotAppId("bot-1");

        IReadOnlyDictionary<string, object?>? merged = BotRequestProperties.Merge(null, overrides);

        Assert.Same(overrides, merged);
    }

    [Fact]
    public void Merge_WithNullOverrides_ReturnsBase()
    {
        IReadOnlyDictionary<string, object?>? baseBag = BotRequestProperties.ForBotAppId("bot-1");

        IReadOnlyDictionary<string, object?>? merged = BotRequestProperties.Merge(baseBag, null);

        Assert.Same(baseBag, merged);
    }

    [Fact]
    public void Merge_WithEmptyBase_ReturnsOverrides()
    {
        IReadOnlyDictionary<string, object?> empty = new Dictionary<string, object?>();
        IReadOnlyDictionary<string, object?>? overrides = BotRequestProperties.ForBotAppId("bot-1");

        IReadOnlyDictionary<string, object?>? merged = BotRequestProperties.Merge(empty, overrides);

        Assert.Same(overrides, merged);
    }

    [Fact]
    public void Merge_WithBothNull_ReturnsNull()
    {
        Assert.Null(BotRequestProperties.Merge(null, null));
    }

    [Fact]
    public void Merge_WithBothEmpty_ReturnsNull()
    {
        IReadOnlyDictionary<string, object?> empty = new Dictionary<string, object?>();

        Assert.Null(BotRequestProperties.Merge(empty, empty));
    }

    // ---- Accessors ---------------------------------------------------------

    [Fact]
    public void GetBotAppId_WhenAbsent_ReturnsNull()
    {
        IReadOnlyDictionary<string, object?> bag = new Dictionary<string, object?>();

        Assert.Null(bag.GetBotAppId());
        Assert.Null(bag.GetAgenticIdentity());
    }

    [Fact]
    public void GetAgenticIdentity_WhenWrongType_ReturnsNull()
    {
        IReadOnlyDictionary<string, object?> bag = new Dictionary<string, object?>
        {
            [BotRequestProperties.AgenticIdentityKey] = "not-an-agentic-identity",
        };

        Assert.Null(bag.GetAgenticIdentity());
    }
}
