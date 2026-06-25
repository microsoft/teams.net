// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Http;

public class BotRequestContextTests
{
    private static AgenticIdentity Agentic(string appId = "agentic-app", string userId = "agentic-user")
        => new() { AgenticAppId = appId, AgenticUserId = userId };

    // ---- FromBotAppId ------------------------------------------------------

    [Fact]
    public void FromBotAppId_WithValue_UsesValueAsIs()
    {
        BotRequestContext? ctx = BotRequestContext.FromBotAppId("28:abc");

        Assert.NotNull(ctx);
        // FromBotAppId does NOT strip the channel prefix; the caller passes the id directly.
        Assert.Equal("28:abc", ctx!.BotAppId);
        Assert.Null(ctx.AgenticIdentity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FromBotAppId_WithNullOrEmpty_ReturnsNull(string? botAppId)
    {
        Assert.Null(BotRequestContext.FromBotAppId(botAppId));
    }

    // ---- FromAgenticIdentity ------------------------------------------------

    [Fact]
    public void FromAgenticIdentity_WithValue_CarriesOnlyAgenticIdentity()
    {
        AgenticIdentity identity = Agentic();

        BotRequestContext? ctx = BotRequestContext.FromAgenticIdentity(identity);

        Assert.NotNull(ctx);
        Assert.Same(identity, ctx!.AgenticIdentity);
        Assert.Null(ctx.BotAppId);
    }

    [Fact]
    public void FromAgenticIdentity_WithNull_ReturnsNull()
    {
        Assert.Null(BotRequestContext.FromAgenticIdentity(null));
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

        BotRequestContext? ctx = BotRequestContext.FromActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("bot-app-id", ctx!.BotAppId);
        Assert.Equal("agentic-app", ctx.AgenticIdentity?.AgenticAppId);
    }

    [Fact]
    public void FromActivity_WithoutChannelPrefix_KeepsIdAsIs()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "plain-bot-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("plain-bot-id", ctx!.BotAppId);
        // No agentic fields on the sender -> no agentic identity.
        Assert.Null(ctx.AgenticIdentity);
    }

    [Fact]
    public void FromActivity_WithNullActivity_ReturnsNull()
    {
        Assert.Null(BotRequestContext.FromActivity(null));
    }

    [Fact]
    public void FromActivity_WithNothingDerivable_ReturnsNull()
    {
        CoreActivity activity = new() { Type = ActivityType.Message, From = new ChannelAccount { Id = "" } };

        Assert.Null(BotRequestContext.FromActivity(activity));
    }

    // ---- FromInboundActivity (inbound: bot app id + agentic from Recipient) -

    [Fact]
    public void FromInboundActivity_TakesBotAppIdAndAgenticFromRecipient()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id" },
            Recipient = new ChannelAccount { Id = "28:recipient-bot-id", AgenticUserId = "agentic-user" },
        };

        BotRequestContext? ctx = BotRequestContext.FromInboundActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("recipient-bot-id", ctx!.BotAppId);
        Assert.Equal("agentic-user", ctx.AgenticIdentity?.AgenticUserId);
    }

    [Fact]
    public void FromInboundActivity_IgnoresAgenticFieldsOnSender()
    {
        // Agentic identity lives on the bot's account (Recipient), not the sender (From).
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id", AgenticUserId = "agentic-user" },
            Recipient = new ChannelAccount { Id = "28:recipient-bot-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromInboundActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("recipient-bot-id", ctx!.BotAppId);
        Assert.Null(ctx.AgenticIdentity);
    }

    // ---- Merge -------------------------------------------------------------

    [Fact]
    public void Merge_OverridesWinOnConflictingFields()
    {
        BotRequestContext? baseCtx = BotRequestContext.FromBotAppId("base-bot");
        BotRequestContext? overrides = BotRequestContext.FromBotAppId("override-bot");

        BotRequestContext? merged = BotRequestContext.Merge(baseCtx, overrides);

        Assert.NotNull(merged);
        Assert.Equal("override-bot", merged!.BotAppId);
    }

    [Fact]
    public void Merge_UnionsDistinctFields()
    {
        AgenticIdentity identity = Agentic();
        BotRequestContext? baseCtx = BotRequestContext.FromBotAppId("bot-1");
        BotRequestContext? overrides = BotRequestContext.FromAgenticIdentity(identity);

        BotRequestContext? merged = BotRequestContext.Merge(baseCtx, overrides);

        Assert.NotNull(merged);
        Assert.Equal("bot-1", merged!.BotAppId);
        Assert.Same(identity, merged.AgenticIdentity);
    }

    [Fact]
    public void Merge_OverridesNullField_DoesNotClobberBase()
    {
        // overrides has only BotAppId set; its null AgenticIdentity must not wipe the base value.
        BotRequestContext baseCtx = new() { AgenticIdentity = Agentic(), BotAppId = "base-bot" };
        BotRequestContext? overrides = BotRequestContext.FromBotAppId("override-bot");

        BotRequestContext? merged = BotRequestContext.Merge(baseCtx, overrides);

        Assert.NotNull(merged);
        Assert.Equal("override-bot", merged!.BotAppId);
        Assert.Same(baseCtx.AgenticIdentity, merged.AgenticIdentity);
    }

    [Fact]
    public void Merge_WithNullBase_ReturnsOverrides()
    {
        BotRequestContext? overrides = BotRequestContext.FromBotAppId("bot-1");

        BotRequestContext? merged = BotRequestContext.Merge(null, overrides);

        Assert.Same(overrides, merged);
    }

    [Fact]
    public void Merge_WithNullOverrides_ReturnsBase()
    {
        BotRequestContext? baseCtx = BotRequestContext.FromBotAppId("bot-1");

        BotRequestContext? merged = BotRequestContext.Merge(baseCtx, null);

        Assert.Same(baseCtx, merged);
    }

    [Fact]
    public void Merge_WithBothNull_ReturnsNull()
    {
        Assert.Null(BotRequestContext.Merge(null, null));
    }

    // ---- ToOptions (typed fields -> option keys) ---------------------------

    [Fact]
    public void ToOptions_YieldsWellKnownKeys()
    {
        AgenticIdentity identity = Agentic();
        BotRequestContext ctx = new() { AgenticIdentity = identity, BotAppId = "bot-1" };

        Dictionary<string, object?> options = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> entry in ctx.ToOptions())
        {
            options[entry.Key] = entry.Value;
        }

        Assert.Same(identity, options[BotRequestContext.AgenticIdentityKey]);
        Assert.Equal("bot-1", options[BotRequestContext.BotAppIdKey]);
    }
}
