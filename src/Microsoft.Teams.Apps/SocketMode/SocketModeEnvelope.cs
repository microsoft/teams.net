// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.SocketMode;

internal static class SocketModeEnvelope
{
    internal static bool TryReadActivity(SocketActivityEnvelope envelope, out CoreActivity? activity)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return TryReadActivityCandidate(envelope.Payload, out activity)
            || TryReadActivityCandidate(envelope.Activity, out activity);
    }

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
