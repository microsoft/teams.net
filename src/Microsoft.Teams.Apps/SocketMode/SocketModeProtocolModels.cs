// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Contains the connection details returned by Socket Mode negotiation.
/// </summary>
internal sealed class SocketModeNegotiateResponse
{
    /// <summary>
    /// Gets the negotiated SignalR endpoint.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>
    /// Gets the access token for the negotiated endpoint.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; init; }

    /// <summary>
    /// Gets the access token lifetime in seconds.
    /// </summary>
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; init; }
}

/// <summary>
/// Identifies an established Socket Mode connection.
/// </summary>
internal sealed class SocketReadyFrame
{
    /// <summary>
    /// Gets the key identifying the bot connection.
    /// </summary>
    [JsonPropertyName("botKey")]
    public string? BotKey { get; init; }

    /// <summary>
    /// Gets the server-assigned connection identifier.
    /// </summary>
    [JsonPropertyName("connectionId")]
    public string? ConnectionId { get; init; }
}

/// <summary>
/// Represents an activity delivered over Socket Mode.
/// </summary>
internal sealed class SocketActivityEnvelope
{
    /// <summary>
    /// Gets the protocol version used by the envelope.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public int? ProtocolVersion { get; init; }

    /// <summary>
    /// Gets the identifier used to correlate replies with the envelope.
    /// </summary>
    [JsonPropertyName("envelopeId")]
    public string? EnvelopeId { get; init; }

    /// <summary>
    /// Gets the envelope type.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// Gets whether the envelope requires a reply.
    /// </summary>
    [JsonPropertyName("ackRequired")]
    public bool? AckRequired { get; init; }

    /// <summary>
    /// Gets the activity payload.
    /// </summary>
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }

    /// <summary>
    /// Gets the activity from the alternate activity field.
    /// </summary>
    [JsonPropertyName("activity")]
    public JsonElement? Activity { get; init; }

    /// <summary>
    /// Gets the correlation vector associated with the envelope.
    /// </summary>
    [JsonPropertyName("cv")]
    public string? CorrelationVector { get; init; }
}

/// <summary>
/// Represents a reply sent for a Socket Mode activity envelope.
/// </summary>
internal sealed class SocketReplyFrame
{
    /// <summary>
    /// Gets the Socket Mode protocol version.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; init; } = SocketModeProtocol.CurrentVersion;

    /// <summary>
    /// Gets the identifier of the envelope being answered.
    /// </summary>
    [JsonPropertyName("envelopeId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EnvelopeId { get; init; }

    /// <summary>
    /// Gets the key identifying the bot connection.
    /// </summary>
    [JsonPropertyName("botKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BotKey { get; init; }

    /// <summary>
    /// Gets the reply status code.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    /// Gets the reply body.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Body { get; init; }

    /// <summary>
    /// Gets the envelope receive time as Unix time in milliseconds.
    /// </summary>
    [JsonPropertyName("recvAt")]
    public long ReceivedAtUnixMilliseconds { get; init; }

    /// <summary>
    /// Gets the reply creation time as Unix time in milliseconds.
    /// </summary>
    [JsonPropertyName("ts")]
    public long TimestampUnixMilliseconds { get; init; }
}

/// <summary>
/// Contains the status and optional body produced by activity dispatch.
/// </summary>
/// <param name="Status">The dispatch status code.</param>
/// <param name="Body">The optional response body.</param>
internal sealed record SocketDispatchResult(int Status, object? Body = null);
