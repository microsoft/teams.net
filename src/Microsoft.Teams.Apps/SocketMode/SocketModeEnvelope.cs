// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Parses Socket Mode envelopes and creates reply frames.
/// </summary>
internal static class SocketModeEnvelope
{
    /// <summary>
    /// Attempts to read a Teams activity from an envelope.
    /// </summary>
    /// <param name="envelope">The envelope to inspect.</param>
    /// <param name="activity">The parsed activity, when successful.</param>
    /// <returns><c>true</c> when the envelope contains a valid activity; otherwise, <c>false</c>.</returns>
    internal static bool TryReadActivity(SocketActivityEnvelope envelope, out CoreActivity? activity)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return TryReadActivityCandidate(envelope.Payload, out activity)
            || TryReadActivityCandidate(envelope.Activity, out activity);
    }

    /// <summary>
    /// Creates an acknowledgement for an activity envelope.
    /// </summary>
    /// <param name="envelope">The envelope being acknowledged.</param>
    /// <param name="botKey">The key identifying the bot connection.</param>
    /// <param name="receivedAtUnixMilliseconds">The time the envelope was received.</param>
    /// <param name="timestampUnixMilliseconds">The time the reply was created.</param>
    /// <param name="status">The acknowledgement status code.</param>
    /// <returns>The acknowledgement frame.</returns>
    internal static SocketReplyFrame CreateAcknowledgement(
        SocketActivityEnvelope envelope,
        string? botKey,
        long receivedAtUnixMilliseconds,
        long timestampUnixMilliseconds,
        int status = 200)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return new SocketReplyFrame
        {
            EnvelopeId = envelope.EnvelopeId,
            BotKey = botKey,
            Status = status,
            ReceivedAtUnixMilliseconds = receivedAtUnixMilliseconds,
            TimestampUnixMilliseconds = timestampUnixMilliseconds,
        };
    }

    /// <summary>
    /// Creates a reply for an invoke activity envelope.
    /// </summary>
    /// <param name="envelope">The envelope being answered.</param>
    /// <param name="botKey">The key identifying the bot connection.</param>
    /// <param name="result">The dispatch result to return.</param>
    /// <param name="receivedAtUnixMilliseconds">The time the envelope was received.</param>
    /// <param name="timestampUnixMilliseconds">The time the reply was created.</param>
    /// <returns>The invoke reply frame.</returns>
    internal static SocketReplyFrame CreateInvokeReply(
        SocketActivityEnvelope envelope,
        string? botKey,
        SocketDispatchResult result,
        long receivedAtUnixMilliseconds,
        long timestampUnixMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(result);

        return new SocketReplyFrame
        {
            EnvelopeId = envelope.EnvelopeId,
            BotKey = botKey,
            Status = result.Status,
            Body = result.Body,
            ReceivedAtUnixMilliseconds = receivedAtUnixMilliseconds,
            TimestampUnixMilliseconds = timestampUnixMilliseconds,
        };
    }

    /// <summary>
    /// Attempts to deserialize an activity candidate from an envelope.
    /// </summary>
    private static bool TryReadActivityCandidate(JsonElement? candidate, out CoreActivity? activity)
    {
        activity = null;

        if (candidate is not { ValueKind: JsonValueKind.Object } element
            || !element.TryGetProperty("type", out JsonElement type)
            || type.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        try
        {
            activity = CoreActivity.FromJsonString(element.GetRawText());
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
