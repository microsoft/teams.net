// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.SocketMode;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeProtocolTests
{
    [Theory]
    [InlineData("""
        {
          "url": "https://signalr.example.test/client",
          "accessToken": "secret",
          "expiresIn": 3600
        }
        """)]
    [InlineData("""
        {
          "Url": "https://signalr.example.test/client",
          "AccessToken": "secret",
          "ExpiresIn": 3600
        }
        """)]
    public void NegotiateResponse_DeserializesPropertyNamesCaseInsensitively(string json)
    {
        SocketModeNegotiateResponse? response = SocketModeJson.Deserialize<SocketModeNegotiateResponse>(json);

        Assert.NotNull(response);
        Assert.Equal("https://signalr.example.test/client", response.Url);
        Assert.Equal("secret", response.AccessToken);
        Assert.Equal(3600, response.ExpiresIn);
    }

    [Theory]
    [InlineData("""
        {
          "protocolVersion": 1,
          "envelopeId": "env-1",
          "type": "message",
          "ackRequired": true,
          "payload": { "type": "message", "text": "hello" },
          "cv": "cv-value"
        }
        """)]
    [InlineData("""
        {
          "ProtocolVersion": 1,
          "EnvelopeId": "env-1",
          "Type": "message",
          "AckRequired": true,
          "Payload": { "type": "message", "text": "hello" },
          "Cv": "cv-value"
        }
        """)]
    public void ActivityEnvelope_DeserializesPropertyNamesCaseInsensitively(string json)
    {
        SocketActivityEnvelope? envelope = SocketModeJson.Deserialize<SocketActivityEnvelope>(json);

        Assert.NotNull(envelope);
        Assert.Equal(1, envelope.ProtocolVersion);
        Assert.Equal("env-1", envelope.EnvelopeId);
        Assert.Equal("message", envelope.Type);
        Assert.True(envelope.AckRequired);
        Assert.Equal("cv-value", envelope.CorrelationVector);
        Assert.Equal(JsonValueKind.Object, envelope.Payload?.ValueKind);
        Assert.Equal("message", envelope.Payload?.GetProperty("type").GetString());
    }

    [Fact]
    public void ActivityEnvelope_PreservesPayloadAndActivityAliases()
    {
        const string json = """
            {
              "payload": { "type": "message" },
              "activity": { "type": "invoke" }
            }
            """;

        SocketActivityEnvelope? envelope = SocketModeJson.Deserialize<SocketActivityEnvelope>(json);

        Assert.NotNull(envelope);
        Assert.Equal("message", envelope.Payload?.GetProperty("type").GetString());
        Assert.Equal("invoke", envelope.Activity?.GetProperty("type").GetString());
    }

    [Fact]
    public void ActivityEnvelope_IgnoresUnknownProperties()
    {
        const string json = """
            {
              "protocolVersion": 1,
              "futureField": "ignored"
            }
            """;

        SocketActivityEnvelope? envelope = SocketModeJson.Deserialize<SocketActivityEnvelope>(json);

        Assert.NotNull(envelope);
        Assert.Equal(1, envelope.ProtocolVersion);
    }

    [Fact]
    public void ReadyFrame_DeserializesPropertyNamesCaseInsensitively()
    {
        const string json = """
            {
              "BotKey": "bot-id",
              "ConnectionId": "connection-id"
            }
            """;

        SocketReadyFrame? frame = SocketModeJson.Deserialize<SocketReadyFrame>(json);

        Assert.NotNull(frame);
        Assert.Equal("bot-id", frame.BotKey);
        Assert.Equal("connection-id", frame.ConnectionId);
    }

    [Fact]
    public void ReplyFrame_SerializesExactWireShape()
    {
        SocketReplyFrame frame = new()
        {
            EnvelopeId = "env-1",
            BotKey = "bot-id",
            Status = 202,
            Body = new { result = "accepted" },
            ReceivedAtUnixMilliseconds = 1_789_999_999_000,
            TimestampUnixMilliseconds = 1_790_000_000_000,
        };

        using JsonDocument document = JsonDocument.Parse(SocketModeJson.Serialize(frame));
        JsonElement root = document.RootElement;

        Assert.Equal(1, root.GetProperty("protocolVersion").GetInt32());
        Assert.Equal("env-1", root.GetProperty("envelopeId").GetString());
        Assert.Equal("bot-id", root.GetProperty("botKey").GetString());
        Assert.Equal(202, root.GetProperty("status").GetInt32());
        Assert.Equal("accepted", root.GetProperty("body").GetProperty("result").GetString());
        Assert.Equal(1_789_999_999_000, root.GetProperty("recvAt").GetInt64());
        Assert.Equal(1_790_000_000_000, root.GetProperty("ts").GetInt64());
    }

    [Fact]
    public void ReplyFrame_OmitsNullableProperties()
    {
        SocketReplyFrame frame = new()
        {
            Status = 200,
            ReceivedAtUnixMilliseconds = 1,
            TimestampUnixMilliseconds = 2,
        };

        using JsonDocument document = JsonDocument.Parse(SocketModeJson.Serialize(frame));
        JsonElement root = document.RootElement;

        Assert.False(root.TryGetProperty("envelopeId", out _));
        Assert.False(root.TryGetProperty("botKey", out _));
        Assert.False(root.TryGetProperty("body", out _));
    }
}
