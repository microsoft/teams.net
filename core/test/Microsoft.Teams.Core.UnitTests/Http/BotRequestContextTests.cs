// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Core.Http;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Core.UnitTests.Http;

public class BotRequestContextTests
{
    private static AgenticUser AgenticUser(string appId = "agent-app-instance", string userId = "agent-user")
        => new() { AgenticAppInstanceId = appId, AgenticUserId = userId };

    // ---- FromBotAppId ------------------------------------------------------

    [Fact]
    public void FromBotAppId_WithValue_UsesValueAsIs()
    {
        BotRequestContext? ctx = BotRequestContext.FromBotAppId("28:abc");

        Assert.NotNull(ctx);
        // FromBotAppId does NOT strip the channel prefix; the caller passes the id directly.
        Assert.Equal("28:abc", ctx!.BotAppId);
        Assert.Null(ctx.AgenticUser);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FromBotAppId_WithNullOrEmpty_ReturnsNull(string? botAppId)
    {
        Assert.Null(BotRequestContext.FromBotAppId(botAppId));
    }

    // ---- FromAgenticUser ------------------------------------------------

    [Fact]
    public void FromAgenticUser_WithValue_CarriesOnlyAgenticUser()
    {
        AgenticUser identity = AgenticUser();

        BotRequestContext? ctx = BotRequestContext.FromAgenticUser(identity);

        Assert.NotNull(ctx);
        Assert.Same(identity, ctx!.AgenticUser);
        Assert.Null(ctx.BotAppId);
    }

    [Fact]
    public void FromAgenticUser_WithNull_ReturnsNull()
    {
        Assert.Null(BotRequestContext.FromAgenticUser(null));
    }

    // ---- FromActivity (outbound: derive from From) -------------------------

    [Fact]
    public void FromActivity_StripsChannelPrefixFromBotId_AndDerivesAgenticUserFromSender()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount
            {
                Id = "28:bot-app-id",
                AgenticAppInstanceId = "agent-app-instance",
                AgenticUserId = "agent-user",
            },
        };

        BotRequestContext? ctx = BotRequestContext.FromActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("bot-app-id", ctx!.BotAppId);
        Assert.Equal("agent-app-instance", ctx.AgenticUser?.AgenticAppInstanceId);
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
        // No agentic user fields on the sender -> no agentic user.
        Assert.Null(ctx.AgenticUser);
    }

    [Fact]
    public void FromActivity_PrefersBotId_WhenBothPresent()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "28:from-account-id", BotId = "28:from-bot-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("from-bot-id", ctx!.BotAppId);
    }

    [Fact]
    public void FromActivity_FallsBackToId_WhenBotIdAbsent()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "28:from-app-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("from-app-id", ctx!.BotAppId);
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

    // ---- FromInboundActivity (inbound: bot app id + agentic user from Recipient) -

    [Fact]
    public void FromInboundActivity_TakesBotAppIdAndAgenticUserFromRecipient()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id" },
            Recipient = new ChannelAccount { Id = "recipient-account-id", BotId = "28:recipient-bot-id", AgenticUserId = "agent-user" },
        };

        BotRequestContext? ctx = BotRequestContext.FromInboundActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("recipient-bot-id", ctx!.BotAppId);
        Assert.Equal("agent-user", ctx.AgenticUser?.AgenticUserId);
    }

    [Fact]
    public void FromInboundActivity_IgnoresAgenticUserFieldsOnSender()
    {
        // Agentic user lives on the bot's account (Recipient), not the sender (From).
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id", AgenticUserId = "agent-user" },
            Recipient = new ChannelAccount { Id = "recipient-account-id", BotId = "28:recipient-bot-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromInboundActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("recipient-bot-id", ctx!.BotAppId);
        Assert.Null(ctx.AgenticUser);
    }

    [Fact]
    public void FromInboundActivity_FallsBackToRecipientId_WhenBotIdAbsent()
    {
        // Standard (non-agent-user) inbound activity: SMBA does not populate BotId, but Recipient.Id
        // carries the Teams-style "28:<appId>" value.
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id" },
            Recipient = new ChannelAccount { Id = "28:recipient-app-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromInboundActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("recipient-app-id", ctx!.BotAppId);
    }

    [Fact]
    public void FromInboundActivity_PrefersBotId_WhenBothPresent()
    {
        CoreActivity activity = new()
        {
            Type = ActivityType.Message,
            From = new ChannelAccount { Id = "user-id" },
            Recipient = new ChannelAccount { Id = "28:recipient-account-id", BotId = "28:recipient-bot-id" },
        };

        BotRequestContext? ctx = BotRequestContext.FromInboundActivity(activity);

        Assert.NotNull(ctx);
        Assert.Equal("recipient-bot-id", ctx!.BotAppId);
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
        AgenticUser identity = AgenticUser();
        BotRequestContext? baseCtx = BotRequestContext.FromBotAppId("bot-1");
        BotRequestContext? overrides = BotRequestContext.FromAgenticUser(identity);

        BotRequestContext? merged = BotRequestContext.Merge(baseCtx, overrides);

        Assert.NotNull(merged);
        Assert.Equal("bot-1", merged!.BotAppId);
        Assert.Same(identity, merged.AgenticUser);
    }

    [Fact]
    public void Merge_OverridesNullField_DoesNotClobberBase()
    {
        // overrides has only BotAppId set; its null AgenticUser must not wipe the base value.
        BotRequestContext baseCtx = new() { AgenticUser = AgenticUser(), BotAppId = "base-bot" };
        BotRequestContext? overrides = BotRequestContext.FromBotAppId("override-bot");

        BotRequestContext? merged = BotRequestContext.Merge(baseCtx, overrides);

        Assert.NotNull(merged);
        Assert.Equal("override-bot", merged!.BotAppId);
        Assert.Same(baseCtx.AgenticUser, merged.AgenticUser);
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
        AgenticUser identity = AgenticUser();
        BotRequestContext ctx = new() { AgenticUser = identity, BotAppId = "bot-1" };

        Dictionary<string, object?> options = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, object?> entry in ctx.ToOptions())
        {
            options[entry.Key] = entry.Value;
        }

        Assert.Same(identity, options[BotRequestContext.AgenticUserKey]);
        Assert.Equal("bot-1", options[BotRequestContext.BotAppIdKey]);
    }
}
