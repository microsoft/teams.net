// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.SocketMode;

internal sealed class SocketModeNegotiateResponse
{
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; init; }
}

internal sealed class SocketReadyFrame
{
    [JsonPropertyName("botKey")]
    public string? BotKey { get; init; }

    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; init; }
}

internal sealed class SocketActivityEnvelope
{
    [JsonPropertyName("protocolVersion")]
    public int? ProtocolVersion { get; init; }

    [JsonPropertyName("envelopeId")]
    public string? EnvelopeId { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("ackRequired")]
    public bool? AckRequired { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }

    [JsonPropertyName("activity")]
    public JsonElement? Activity { get; init; }

    [JsonPropertyName("cv")]
    public string? CorrelationVector { get; init; }
}

internal sealed class SocketReplyFrame
{
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; init; } = SocketModeProtocol.CurrentVersion;

    [JsonPropertyName("envelopeId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EnvelopeId { get; init; }

    [JsonPropertyName("botKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BotKey { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; init; }

    [JsonPropertyName("recvAt")]
    public long ReceivedAtUnixMilliseconds { get; init; }

    [JsonPropertyName("ts")]
    public long TimestampUnixMilliseconds { get; init; }
}

internal sealed record SocketDispatchResult(int Status, object? Body = null);
