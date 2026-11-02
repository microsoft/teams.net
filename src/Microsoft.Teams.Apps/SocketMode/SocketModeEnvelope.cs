// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Teams.Core.Schema;

namespace Microsoft.Teams.Apps.SocketMode;

/// <summary>
/// Parses Socket Mode envelopes and creates reply frames.
/// </summary>
internal static class SocketModeEnvelope
{
    private const string AgenticUserRole = "agenticUser";
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
    /// Validates an envelope received on an agentic connection, without changing the recipient identity that replies
    /// act as.
    /// </summary>
    /// <param name="envelope">The envelope being validated.</param>
    /// <param name="activity">The activity read from the envelope.</param>
    /// <param name="blueprintId">The bot's client ID, which is the agent blueprint ID.</param>
    /// <param name="error">The reason the envelope is rejected, when validation fails.</param>
    /// <returns><c>true</c> when any bot key matches and the recipient is a complete agentic identity for the blueprint.</returns>
    internal static bool TryValidateAgenticEnvelope(
        SocketActivityEnvelope envelope,
        CoreActivity activity,
        string? blueprintId,
        [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(activity);

        if (envelope.BotKey is not null && !IdEquals(envelope.BotKey, blueprintId))
        {
            error = "envelope botKey does not match the bot client ID";
            return false;
        }

        return TryValidateAgenticRecipient(activity.Recipient, blueprintId, out error);
    }

    /// <summary>
    /// Validates that a recipient is a complete agentic identity for the given blueprint.
    /// </summary>
    /// <param name="recipient">The activity recipient.</param>
    /// <param name="blueprintId">The bot's client ID, which is the agent blueprint ID.</param>
    /// <param name="error">The reason the recipient is rejected, when validation fails.</param>
    /// <returns><c>true</c> when the recipient names a blueprint, agentic app, and tenant, plus an agentic user for an
    /// Agentic User recipient, and its blueprint matches <paramref name="blueprintId"/>.</returns>
    internal static bool TryValidateAgenticRecipient(
        ChannelAccount? recipient,
        string? blueprintId,
        [NotNullWhen(false)] out string? error)
    {
        if (recipient is null)
        {
            error = "agentic activity requires a recipient identity";
            return false;
        }

        bool requiresUser = IsAgenticUserRole(recipient) || recipient.AgenticUserId is not null;
        if (!IsPresentAndUnpadded(recipient.AgenticAppBlueprintId)
            || !IsPresentAndUnpadded(recipient.AgenticAppId)
            || !IsPresentAndUnpadded(recipient.TenantId)
            || (requiresUser && !IsPresentAndUnpadded(recipient.AgenticUserId)))
        {
            error = requiresUser
                ? "agentic recipient requires non-empty, unpadded agenticAppBlueprintId, agenticAppId, tenantId, and agenticUserId"
                : "agentic recipient requires non-empty, unpadded agenticAppBlueprintId, agenticAppId, and tenantId";
            return false;
        }

        if (!IdEquals(recipient.AgenticAppBlueprintId, blueprintId))
        {
            error = "agentic recipient blueprint does not match the bot client ID";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsAgenticUserRole(ChannelAccount recipient)
        => recipient.Properties.TryGetValue("role", out object? role)
            && role switch
            {
                string value => value == AgenticUserRole,
                JsonElement { ValueKind: JsonValueKind.String } element => element.ValueEquals(AgenticUserRole),
                _ => false,
            };

    private static bool IsPresentAndUnpadded([NotNullWhen(true)] string? value)
        => !string.IsNullOrEmpty(value) && value.Trim().Length == value.Length;

    // App and blueprint IDs are GUIDs, compared case-insensitively like the rest of the SDK's app ID checks.
    private static bool IdEquals(string? value, string? expected)
        => expected is not null && string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

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
