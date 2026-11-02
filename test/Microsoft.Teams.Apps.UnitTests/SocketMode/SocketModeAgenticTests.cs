// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeAgenticTests
{
    private const string BlueprintId = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public void AgenticSettings_DefaultToClassicMode()
    {
        SocketModeOptions options = new();

        Assert.Null(options.AgenticAppId);
        Assert.Null(options.AgenticTokenScope);
        Assert.Null(options.AgenticTenantId);
        options.ValidateAgenticSettings();
        Assert.False(options.ToTransportOptions().RequireAgenticRecipient);
    }

    [Fact]
    public void AgenticSettings_WithAppIdAndScope_SelectAgenticTransport()
    {
        SocketModeOptions options = new() { AgenticAppId = "agent-instance", AgenticTokenScope = "api://socket/.default" };

        options.ValidateAgenticSettings();

        Assert.True(options.ToTransportOptions().RequireAgenticRecipient);
    }

    public static TheoryData<string?, string?, string?> InvalidAgenticSettings => new()
    {
        { null, "api://socket/.default", null },
        { null, null, "tenant" },
        { "", "api://socket/.default", null },
        { " ", "api://socket/.default", null },
        { "agent-instance", null, null },
        { "agent-instance", "", null },
        { "agent-instance", " ", "tenant" },
    };

    [Theory]
    [MemberData(nameof(InvalidAgenticSettings))]
    public void AgenticSettings_RejectIncompleteConfiguration(string? appId, string? scope, string? tenantId)
    {
        SocketModeOptions options = new() { AgenticAppId = appId, AgenticTokenScope = scope, AgenticTenantId = tenantId };

        Assert.Throws<ArgumentException>(options.ValidateAgenticSettings);
    }

    [Theory]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant","agenticUserId":"user","role":"agenticUser"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant","role":"agenticAppInstance"}""")]
    public void TryValidateAgenticRecipient_AcceptsCompleteIdentity(string recipient)
    {
        Assert.True(SocketModeEnvelope.TryValidateAgenticRecipient(Recipient(recipient), BlueprintId, out string? error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateAgenticRecipient_ComparesTheBlueprintCaseInsensitively()
    {
        ChannelAccount recipient = Recipient("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant"}""");

        Assert.True(SocketModeEnvelope.TryValidateAgenticRecipient(recipient, BlueprintId.ToUpperInvariant(), out _));
    }

    [Theory]
    [InlineData("""{"agenticAppId":"instance","tenantId":"tenant"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","tenantId":"tenant"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"","tenantId":"tenant"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":" instance","tenantId":"tenant"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant "}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant","role":"agenticUser"}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant","agenticUserId":""}""")]
    [InlineData("""{"agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","tenantId":"tenant","agenticUserId":" user"}""")]
    public void TryValidateAgenticRecipient_RejectsIncompleteOrPaddedIdentity(string recipient)
    {
        Assert.False(SocketModeEnvelope.TryValidateAgenticRecipient(Recipient(recipient), BlueprintId, out string? error));
        Assert.Contains("non-empty, unpadded", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidateAgenticRecipient_RejectsMissingRecipient()
    {
        Assert.False(SocketModeEnvelope.TryValidateAgenticRecipient(null, BlueprintId, out string? error));
        Assert.Equal("agentic activity requires a recipient identity", error);
    }

    [Theory]
    [InlineData(BlueprintId, "22222222-2222-2222-2222-222222222222")]
    [InlineData(BlueprintId, null)]
    public void TryValidateAgenticRecipient_RejectsBlueprintMismatch(string recipientBlueprint, string? clientId)
    {
        ChannelAccount recipient = new() { AgenticAppBlueprintId = recipientBlueprint, AgenticAppId = "instance", TenantId = "tenant" };

        Assert.False(SocketModeEnvelope.TryValidateAgenticRecipient(recipient, clientId, out string? error));
        Assert.Equal("agentic recipient blueprint does not match the bot client ID", error);
    }

    [Fact]
    public void TryValidateAgenticRecipient_DoesNotChangeTheIdentity()
    {
        ChannelAccount recipient = Recipient("""{"id":"agent","agenticAppBlueprintId":"11111111-1111-1111-1111-111111111111","agenticAppId":"instance","agenticUserId":"user","tenantId":"tenant","role":"agenticUser"}""");

        SocketModeEnvelope.TryValidateAgenticRecipient(recipient, BlueprintId, out _);

        AgenticIdentity identity = Assert.IsType<AgenticIdentity>(recipient.GetAgenticIdentity());
        Assert.Equal(BlueprintId, identity.AgenticAppBlueprintId);
        Assert.Equal("instance", identity.AgenticAppId);
        Assert.Equal("user", identity.AgenticUserId);
        Assert.Equal("tenant", identity.TenantId);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(BlueprintId, true)]
    [InlineData("22222222-2222-2222-2222-222222222222", false)]
    public void TryValidateAgenticEnvelope_ChecksAnyBotKeyBeforeTheRecipient(string? botKey, bool valid)
    {
        SocketActivityEnvelope envelope = new() { EnvelopeId = "env-1", BotKey = botKey };
        CoreActivity activity = new()
        {
            Type = "message",
            Recipient = new() { AgenticAppBlueprintId = BlueprintId, AgenticAppId = "instance", TenantId = "tenant" },
        };

        Assert.Equal(valid, SocketModeEnvelope.TryValidateAgenticEnvelope(envelope, activity, BlueprintId, out string? error));
        if (!valid)
        {
            Assert.Equal("envelope botKey does not match the bot client ID", error);
        }
    }

    [Fact]
    public void Envelope_DeserializesBotKey()
    {
        SocketActivityEnvelope? envelope = SocketModeJson.Deserialize<SocketActivityEnvelope>("""{"envelopeId":"env-1","botKey":"bot"}""");

        Assert.Equal("bot", envelope?.BotKey);
    }

    private static ChannelAccount Recipient(string json)
        => JsonSerializer.Deserialize<ChannelAccount>(json)!;
}
