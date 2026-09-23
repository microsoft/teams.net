// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.SocketMode;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.UnitTests.SocketMode;

public class SocketModeEnvelopeTests
{
    [Fact]
    public void TryReadActivity_ReadsPayload()
    {
        SocketActivityEnvelope envelope = DeserializeEnvelope("""
            {
              "payload": {
                "type": "message",
                "id": "activity-1",
                "serviceUrl": "https://smba.trafficmanager.net/teams/",
                "text": "hello"
              }
            }
            """);

        bool found = SocketModeEnvelope.TryReadActivity(envelope, out CoreActivity? activity);

        Assert.True(found);
        Assert.NotNull(activity);
        Assert.Equal("message", activity.Type);
        Assert.Equal("activity-1", activity.Id);
        Assert.Equal(new Uri("https://smba.trafficmanager.net/teams/"), activity.ServiceUrl);
        Assert.Equal("hello", activity.Properties.Get<string>("text"));
    }

    [Fact]
    public void TryReadActivity_ReadsActivityAlias()
    {
        SocketActivityEnvelope envelope = DeserializeEnvelope("""
            {
              "activity": {
                "type": "invoke",
                "id": "activity-1"
              }
            }
            """);

        bool found = SocketModeEnvelope.TryReadActivity(envelope, out CoreActivity? activity);

        Assert.True(found);
        Assert.NotNull(activity);
        Assert.Equal("invoke", activity.Type);
        Assert.Equal("activity-1", activity.Id);
    }

    [Fact]
    public void TryReadActivity_PrefersValidPayload()
    {
        SocketActivityEnvelope envelope = DeserializeEnvelope("""
            {
              "payload": {
                "type": "message",
                "id": "payload-activity"
              },
              "activity": {
                "type": "invoke",
                "id": "activity-alias"
              }
            }
            """);

        bool found = SocketModeEnvelope.TryReadActivity(envelope, out CoreActivity? activity);

        Assert.True(found);
        Assert.Equal("payload-activity", activity?.Id);
    }

    [Theory]
    [InlineData("\"not-an-object\"")]
    [InlineData("[]")]
    [InlineData("""{ "id": "missing-type" }""")]
    [InlineData("""{ "type": 42 }""")]
    [InlineData("""{ "type": "message", "id": 42 }""")]
    public void TryReadActivity_FallsBackWhenPayloadIsMalformed(string malformedPayload)
    {
        SocketActivityEnvelope envelope = DeserializeEnvelope($$"""
            {
              "payload": {{malformedPayload}},
              "activity": {
                "type": "message",
                "id": "fallback-activity"
              }
            }
            """);

        bool found = SocketModeEnvelope.TryReadActivity(envelope, out CoreActivity? activity);

        Assert.True(found);
        Assert.Equal("fallback-activity", activity?.Id);
    }

    [Fact]
    public void TryReadActivity_ReturnsFalseWhenNeitherCandidateIsValid()
    {
        SocketActivityEnvelope envelope = DeserializeEnvelope("""
            {
              "payload": { "id": "missing-type" },
              "activity": null
            }
            """);

        bool found = SocketModeEnvelope.TryReadActivity(envelope, out CoreActivity? activity);

        Assert.False(found);
        Assert.Null(activity);
    }

    [Fact]
    public void CreateAcknowledgement_UsesEnvelopeIdentityAndHasNoBody()
    {
        SocketActivityEnvelope envelope = new() { EnvelopeId = "env-1" };

        SocketReplyFrame reply = SocketModeEnvelope.CreateAcknowledgement(
            envelope,
            "bot-id",
            receivedAtUnixMilliseconds: 100,
            timestampUnixMilliseconds: 200,
            status: 202);

        Assert.Equal(SocketModeProtocol.CurrentVersion, reply.ProtocolVersion);
        Assert.Equal("env-1", reply.EnvelopeId);
        Assert.Equal("bot-id", reply.BotKey);
        Assert.Equal(202, reply.Status);
        Assert.Null(reply.Body);
        Assert.Equal(100, reply.ReceivedAtUnixMilliseconds);
        Assert.Equal(200, reply.TimestampUnixMilliseconds);
    }

    [Fact]
    public void CreateInvokeReply_UsesDispatchResult()
    {
        SocketActivityEnvelope envelope = new() { EnvelopeId = "env-1" };
        SocketDispatchResult result = new(201, new { result = "created" });

        SocketReplyFrame reply = SocketModeEnvelope.CreateInvokeReply(
            envelope,
            "bot-id",
            result,
            receivedAtUnixMilliseconds: 100,
            timestampUnixMilliseconds: 200);

        Assert.Equal(SocketModeProtocol.CurrentVersion, reply.ProtocolVersion);
        Assert.Equal("env-1", reply.EnvelopeId);
        Assert.Equal("bot-id", reply.BotKey);
        Assert.Equal(201, reply.Status);
        Assert.Same(result.Body, reply.Body);
        Assert.Equal(100, reply.ReceivedAtUnixMilliseconds);
        Assert.Equal(200, reply.TimestampUnixMilliseconds);
    }

    [Fact]
    public void CreateInvokeReply_AllowsNoBody()
    {
        SocketActivityEnvelope envelope = new() { EnvelopeId = "env-1" };

        SocketReplyFrame reply = SocketModeEnvelope.CreateInvokeReply(
            envelope,
            botKey: null,
            new SocketDispatchResult(204),
            receivedAtUnixMilliseconds: 100,
            timestampUnixMilliseconds: 200);

        Assert.Equal(204, reply.Status);
        Assert.Null(reply.Body);
    }

    private static SocketActivityEnvelope DeserializeEnvelope(string json)
        => SocketModeJson.Deserialize<SocketActivityEnvelope>(json)
            ?? throw new InvalidOperationException("Expected a Socket Mode envelope.");
}
